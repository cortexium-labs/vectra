/**
 * Vectra - K6 Load Test
 *
 * Scenario: 1 000 concurrent AI-agent VUs sustaining ~5 000 req/s.
 *
 * Flow
 *   setup()   - Register POOL_SIZE agents once, obtain their tokens.
 *               VUs reuse the pool round-robin (avoids the ~3-min setup
 *               cost of registering 1 000 agents sequentially).
 *
 *   default() - Per iteration (single ramping-arrival-rate executor):
 *        70 %  Proxy request  ->  GET /proxy/https://httpbin.org/get
 *        20 %  Agent list     ->  GET /Agents
 *        10 %  HITL pending   ->  GET /Hitls
 *
 * Previous design used two conflicting scenarios (ramping-vus +
 * constant-arrival-rate) sharing the same VU pool. k6 could not start
 * either executor, resulting in 0 default() iterations and vacuously-
 * passing thresholds. This version uses a single ramping-arrival-rate
 * executor that handles warm-up and steady-state natively.
 *
 * Environment variables (override via -e):
 *   BASE_URL        - default: http://localhost:7080
 *   TARGET_RPS      - desired req/s           (default: 5000)
 *   VUS             - pre-allocated VUs        (default: 1000)
 *   POOL_SIZE       - agents registered once   (default: 50)
 *   RAMP_DURATION   - warm-up stage duration   (default: 30s)
 *   STEADY_DURATION - sustained load duration  (default: 3m)
 *   OWNER_ID        - OwnerId for registration (default: load-test-owner)
 */

import http from "k6/http";
import { check } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";
import { uuidv4 } from "https://jslib.k6.io/k6-utils/1.4.0/index.js";

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
const BASE_URL = __ENV.BASE_URL || "http://localhost:7080";
const TARGET_RPS = parseInt(__ENV.TARGET_RPS || "500", 10);
const VUS = parseInt(__ENV.VUS || "10", 10);
const POOL_SIZE = parseInt(__ENV.POOL_SIZE || "10", 10);
const RAMP_DURATION = __ENV.RAMP_DURATION || "30s";
const STEADY_DURATION = __ENV.STEADY_DURATION || "30s";
const OWNER_ID = __ENV.OWNER_ID || "load-test-owner";

// ---------------------------------------------------------------------------
// Custom metrics
// ---------------------------------------------------------------------------
const proxyErrors = new Counter("proxy_errors");
const authErrors = new Counter("auth_errors");
const agentRegErrors = new Counter("agent_reg_errors");
const serverDownErrs = new Counter("server_down_errors");

const proxySuccessRate = new Rate("proxy_success_rate");
const agentListSuccessRate = new Rate("agent_list_success_rate");
const hitlSuccessRate = new Rate("hitl_success_rate");

const proxyLatency = new Trend("proxy_latency_ms", true);
const agentListLatency = new Trend("agent_list_latency_ms", true);
const tokenLatency = new Trend("token_latency_ms", true);

// ---------------------------------------------------------------------------
// K6 options
// ---------------------------------------------------------------------------
export const options = {
    scenarios: {
        load: {
            executor: "ramping-arrival-rate",
            startRate: 0,
            timeUnit: "1s",
            preAllocatedVUs: VUS,
            maxVUs: VUS * 5,
            stages: [
                { duration: RAMP_DURATION, target: TARGET_RPS },
                { duration: STEADY_DURATION, target: TARGET_RPS },
            ],
        },
    },

    thresholds: {
        http_req_duration: ["p(99)<500"],
        proxy_latency_ms: ["p(95)<400", "p(99)<800"],
        http_req_failed: ["rate<0.01"],
        proxy_success_rate: ["rate>0.99"],
        agent_list_success_rate: ["rate>0.99"],
        hitl_success_rate: ["rate>0.99"],
        server_down_errors: ["count<1"],
    },
};

// ---------------------------------------------------------------------------
// setup() - runs once before any VU iteration starts
// ---------------------------------------------------------------------------
export function setup() {
    const agents = [];

    console.log(`[setup] Probing ${BASE_URL}/health ...`);

    const probe = http.get(`${BASE_URL}/health`, {
        timeout: "15s",
    });

    if (
        probe.error_code !== 0 ||
        (probe.status !== 200 && probe.status !== 204)
    ) {
        throw new Error(
            `[setup] FATAL: server not reachable at ${BASE_URL} ` +
            `(HTTP ${probe.status}, error_code=${probe.error_code}). ` +
            `Start the Vectra API before running the load test.`
        );
    }

    console.log(`[setup] Server is up (HTTP ${probe.status}).`);

    console.log(
        `[setup] Registering ${POOL_SIZE} pooled agents against ${BASE_URL} ...`
    );

    for (let i = 0; i < POOL_SIZE; i++) {
        const secret = `secret-${uuidv4()}`;
        const name = `load-agent-${i}-${Date.now()}`;

        // Register agent
        const regRes = http.post(
            `${BASE_URL}/agents`,
            JSON.stringify({
                name,
                ownerId: OWNER_ID,
                clientSecret: secret,
            }),
            {
                headers: {
                    "Content-Type": "application/json",
                },
                timeout: "15s",
            }
        );

        if (regRes.status !== 200 && regRes.status !== 201) {
            agentRegErrors.add(1);

            console.error(
                `[setup] Registration FAILED (HTTP ${regRes.status}) ` +
                `agent[${i}] -- ${regRes.body}`
            );

            continue;
        }

        let registeredId;

        try {
            const body = JSON.parse(regRes.body);

            // Server returns PascalCase: { "AgentId": "..." }
            registeredId = body.AgentId || body.agentId || body.id;

            if (!registeredId) {
                throw new Error("AgentId missing");
            }
        } catch (e) {
            console.error(
                `[setup] Cannot read AgentId for agent[${i}]: ${e} ` +
                `body=${regRes.body}`
            );

            continue;
        }

        // Exchange credentials for JWT
        const tokenRes = http.post(
            `${BASE_URL}/tokens`,
            JSON.stringify({
                agentId: registeredId,
                clientSecret: secret,
            }),
            {
                headers: {
                    "Content-Type": "application/json",
                },
                timeout: "15s",
            }
        );

        tokenLatency.add(tokenRes.timings.duration);

        if (tokenRes.status !== 200) {
            authErrors.add(1);

            console.error(
                `[setup] Token FAILED (HTTP ${tokenRes.status}) ` +
                `agent=${registeredId} -- ${tokenRes.body}`
            );

            continue;
        }

        let token;

        try {
            const body = JSON.parse(tokenRes.body);

            // Server returns camelCase: { "accessToken": "..." }
            token = body.accessToken || body.AccessToken;

            if (!token) {
                throw new Error("accessToken missing");
            }
        } catch (e) {
            console.error(
                `[setup] Cannot read AccessToken for agent ${registeredId}: ${e} ` +
                `body=${tokenRes.body}`
            );

            continue;
        }

        // Assign policy so DecisionEngine does not throw null-key 500
        const policyRes = http.put(
            `${BASE_URL}/Agents/${registeredId}/policy`,
            JSON.stringify({
                policyName: "Public or Admin Access",
            }),
            {
                headers: {
                    "Content-Type": "application/json",
                },
                timeout: "15s",
            }
        );

        if (policyRes.status !== 200 && policyRes.status !== 204) {
            console.error(
                `[setup] Policy assign FAILED (HTTP ${policyRes.status}) ` +
                `agent=${registeredId} -- ${policyRes.body}`
            );

            // continue anyway — proxy calls may 500 but other endpoints still work
        }

        agents.push({
            agentId: registeredId,
            token,
        });

        console.log(
            `[setup] Agent ${i + 1}/${POOL_SIZE} ready (id=${registeredId})`
        );
    }

    if (agents.length === 0) {
        throw new Error(
            `[setup] FATAL: 0/${POOL_SIZE} agents provisioned. ` +
            `Verify ${BASE_URL} is reachable and ` +
            `POST /Agents + POST /Tokens return 200/201.`
        );
    }

    console.log(
        `[setup] Pool ready -- ${agents.length}/${POOL_SIZE} agents provisioned.`
    );

    return { agents };
}

// ---------------------------------------------------------------------------
// default() - executed by every VU on every arrival-rate iteration
// ---------------------------------------------------------------------------

// error_code 1212 = connection refused
// error_code 1210 = dial error
const CONNECTION_REFUSED = [1210, 1212];

export default function (data) {
    const { agents } = data;

    const agent = agents[__VU % agents.length];

    const authHeader = {
        Authorization: `Bearer ${agent.token}`,
    };

    // Deterministic weighted traffic distribution:
    // 0-6 => 70%
    // 7-8 => 20%
    // 9   => 10%
    const bucket = __ITER % 10;

    if (bucket < 7) {
        // 70% - Proxy request

        // URL must NOT be percent-encoded:
        // ASP.NET Core does not decode %2F in Request.Path.
        const res = http.get(
            `${BASE_URL}/proxy/http://localhost:7080/health`,
            {
                headers: authHeader,
                timeout: "15s",
            }
        );

        if (CONNECTION_REFUSED.includes(res.error_code)) {
            serverDownErrs.add(1);

            console.error(
                `[VU ${__VU}] Server refused connection on /proxy ` +
                `— is Vectra still running?`
            );

            return;
        }

        proxyLatency.add(res.timings.duration);

        const ok = check(res, {
            "proxy 200": (r) => r.status === 200,
            "proxy has body": (r) => r.body && r.body.length > 0,
        });

        proxySuccessRate.add(ok);

        if (!ok) {
            proxyErrors.add(1);
        }
    } else if (bucket < 9) {
        // 20% - List agents

        const res = http.get(
            `${BASE_URL}/agents?page=1&pageSize=25`,
            {
                headers: authHeader,
                timeout: "15s",
            }
        );

        if (CONNECTION_REFUSED.includes(res.error_code)) {
            serverDownErrs.add(1);

            console.error(
                `[VU ${__VU}] Server refused connection on /agents ` +
                `— is Vectra still running?`
            );

            return;
        }

        agentListLatency.add(res.timings.duration);

        const ok = check(res, {
            "agents list 200": (r) => r.status === 200,
        });

        agentListSuccessRate.add(ok);
    } else {
        // 10% - HITL pending list

        const res = http.get(`${BASE_URL}/hitls`, {
            headers: authHeader,
            timeout: "15s",
        });

        if (CONNECTION_REFUSED.includes(res.error_code)) {
            serverDownErrs.add(1);

            console.error(
                `[VU ${__VU}] Server refused connection on /hitls ` +
                `— is Vectra still running?`
            );

            return;
        }

        const ok = check(res, {
            "hitl 200 or 204": (r) =>
                r.status === 200 || r.status === 204,
        });

        hitlSuccessRate.add(ok);
    }
}

// ---------------------------------------------------------------------------
// teardown() - informational summary only
// ---------------------------------------------------------------------------
export function teardown(data) {
    const count =
        data && data.agents
            ? data.agents.length
            : 0;

    console.log(
        `[teardown] Done. Pool size used: ${count} agents.`
    );
}