# Vectra – K6 Load Test

Simulates **1 000 concurrent AI-agent VUs** sustaining **~5 000 req/s** against a running Vectra instance.

## Prerequisites

| Tool | Version |
|------|---------|
| [k6](https://k6.io/docs/get-started/installation/) | ≥ 0.50 |
| Vectra API | running and reachable |

Install k6 on Windows:
```powershell
winget install k6 --source winget
# or
choco install k6
```

---

## Quick Start

```powershell
# From repo root
k6 run tests/load/k6/vectra_load_test.js
```

All defaults target `http://localhost:7080` with 1 000 VUs / 5 000 req/s.

---

## Configuration via Environment Variables

| Variable           | Default                    | Description                        |
|--------------------|----------------------------|------------------------------------|
| `BASE_URL`         | `http://localhost:7080`    | Vectra API base URL                |
| `TARGET_RPS`       | `5000`                     | Target requests per second         |
| `VUS`              | `1000`                     | Number of concurrent virtual users |
| `RAMP_DURATION`    | `30s`                      | Ramp-up period before steady state |
| `STEADY_DURATION`  | `3m`                       | Duration of the sustained load     |
| `OWNER_ID`         | `load-test-owner`          | OwnerId used when registering agents |

Override via `-e` flags:

```powershell
k6 run `
  -e BASE_URL=https://vectra.internal `
  -e VUS=500 `
  -e TARGET_RPS=2500 `
  -e STEADY_DURATION=10m `
  tests/load/vectra_load_test_without_policy.js
```

---

## Test Phases

```
0s ──── RAMP (30s, 0→1000 VUs) ──── 30s ──── STEADY STATE (3m, ~5000 req/s) ──── ~3m30s
```

### Request Distribution (per iteration)

| Weight | Endpoint                              | Method |
|--------|---------------------------------------|--------|
| 70 %   | `/proxy/https://httpbin.org/get`      | GET    |
| 20 %   | `/agents?page=1&pageSize=25`          | GET    |
| 10 %   | `/hitls`                              | GET    |

---

## Thresholds (test fails if breached)

| Metric                    | Threshold           |
|---------------------------|---------------------|
| `http_req_duration p(99)` | < 500 ms            |
| `proxy_latency_ms p(95)`  | < 400 ms            |
| `proxy_latency_ms p(99)`  | < 800 ms            |
| `http_req_failed`         | rate < 1 %          |
| `proxy_success_rate`      | rate > 99 %         |
| `agent_list_success_rate` | rate > 99 %         |
| `hitl_success_rate`       | rate > 99 %         |

---

## HTML Report

```powershell
k6 run --out json=results.json tests/Load/vectra_load_test_without_policy.js
# then open with k6 reporter or Grafana k6 dashboard
```

---

## CI Integration (GitHub Actions example)

```yaml
- name: Run K6 load test
  uses: grafana/k6-action@v0.3.1
  with:
    filename: tests/Load/vectra_load_test_without_policy.js
  env:
    BASE_URL: ${{ secrets.STAGING_URL }}
    TARGET_RPS: "1000"
    VUS: "200"
    STEADY_DURATION: "1m"
```
