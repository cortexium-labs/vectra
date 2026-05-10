<div align="center">
  <img src="/img/banner.png" alt="Vectra Banner" />
  <h2>VECTRA — Intent-Aware Governance Gateway</h2>

  [![dotnet][dotnet-budge]][dotnet-url]
  [![Build Status][actions-badge]][actions-url]
  [![Quality Gate Status][sonarcloud-quality-gate-badge]][sonarcloud-quality-gate-url]
  [![Reliability Gate Status][sonarcloud-reliability-gate-badge]][sonarcloud-reliability-gate-url]
  [![Maintainability Gate Status][sonarcloud-maintainability-gate-badge]][sonarcloud-maintainability-gate-url]
  [![Security Gate Status][sonarcloud-security-gate-badge]][sonarcloud-security-gate-url]
  [![Vulnerabilities Gate Status][sonarcloud-vulnerabilities-gate-badge]][sonarcloud-vulnerabilities-gate-url]
  [![License: Apache 2.0][apache-badge]][apache-url]
  [![FOSSA License Status][fossa-license-badge]][fossa-license-url]
  [![FOSSA Security Status][fossa-security-badge]][fossa-security-url]
  [![Good First Issues][github-good-first-issue-badge]][github-good-first-issue-url]
</div>

## Overview: VECTRA

**VECTRA** is an open-source **Intent-Aware Governance Gateway** designed to secure, monitor, and control interactions between autonomous agents, services, and complex systems. 

While traditional API gateways route traffic based on static endpoints and basic authentication, VECTRA introduces a semantic layer of security by evaluating the *actual intent* behind every API call. This allows developers to establish dynamic guardrails, ensuring that AI agents and automated systems operate strictly within defined behavioral boundaries.

### Key Capabilities

* **Intent-Based Policy Enforcement:** Move beyond standard Role-Based Access Control (RBAC). VECTRA analyzes the underlying purpose of a request, allowing you to build context-aware policies that govern *what* an agent is trying to achieve, rather than just *who* the agent is.
* **Human-in-the-Loop (HITL) Safeguards:** Not all automated actions should happen instantly. When VECTRA identifies an agent's intent as high-risk, potentially destructive, or malicious, it automatically intercepts the request. The gateway holds the action and routes it to a human operator for manual review and approval before execution.
* **Precise Agent Governance:** As AI agents become more autonomous, the risk of unintended actions grows. VECTRA provides the fine-grained control necessary to oversee agent behavior, preventing systemic damage and ensuring compliance.

### Why VECTRA?
As organizations deploy more LLM-driven agents and complex microservices, establishing trust in automated workflows is critical. VECTRA bridges the gap between automation and safety, providing the necessary oversight to let agents act freely while keeping humans firmly in control of critical decisions.

## Key Features

* ✅**Semantic Intent Analysis:** Evaluates the underlying purpose of every request using natural language understanding, going far beyond simple endpoint matching.
* ✅ **Dynamic Policy Enforcement:** Define and apply context-aware governance rules that adapt to agent behavior and request semantics in real time.
* ✅ **Human-in-the-Loop (HITL):** Automatically intercepts high-risk or ambiguous requests and holds them for manual operator review before execution.
* ✅ **Agent Governance:** Provides fine-grained controls to monitor, restrict, and audit autonomous AI agent actions across your systems.
* ✅ **Audit & Observability:** Maintains a full audit trail of agent intent classifications, policy decisions, and HITL review outcomes.
* ✅ **High-Performance Gateway:** Designed for low-latency interception with minimal overhead, keeping your automated workflows fast and responsive.

## Quick Start

### Run via Docker

The fastest way to get VECTRA running is with Docker:

```bash
docker pull ghcr.io/cortexiumlabs/vectra:latest
docker run -p 708:7080 ghcr.io/cortexiumlabs/vectra:latest
```

VECTRA will be available at `http://localhost:7080`.

To supply your own configuration, mount a config file:

```bash
docker run -p 7080:7080 \
  -v $(pwd)/vectra.json:/app/vectra.json \
  ghcr.io/cortexiumlabs/vectra:latest
```

### Use a Pre-Built Binary

Pre-built binaries for Linux, macOS, and Windows are available on the [Releases](https://github.com/cortexiumlabs/vectra/releases) page.

1. Download the archive for your platform.
2. Extract and make the binary executable (Linux/macOS):

```bash
tar -xzf vectra-<version>-linux-x64.tar.gz
chmod +x vectra
./vectra
```

3. On Windows, run the extracted executable directly:

```powershell
.\vectra.exe
```

### Build from Source

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

```bash
# Clone the repository
git clone https://github.com/cortexiumlabs/vectra.git
cd vectra

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the gateway
dotnet run --project src/Vectra.Gateway --configuration Release
```

To run the full test suite before running:

```bash
dotnet test --configuration Release
```

## License

Vectra is open-source and licensed under the **Apache 2.0 License**.  
See [LICENSE](https://github.com/cortexiumlabs/vectra/blob/main/LICENSE) for details.

## Support Vectra
[![⭐ Star on GitHub](https://img.shields.io/badge/⭐%20Star%20on%20GitHub-555555?style=flat&logo=github)](https://github.com/cortexiumlabs/vectra)  
✨ **Support Vectra by giving it a star!** ✨  
Your support helps others discover the project and drives continued innovation.

[dotnet-budge]: https://img.shields.io/badge/.NET-10.0-green
[dotnet-url]: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
[apache-badge]: https://img.shields.io/badge/License-Apache%202.0-blue.svg?style=flat&logo=github
[apache-url]: https://opensource.org/licenses/Apache-2.0
[actions-badge]: https://github.com/cortexiumlabs/vectra/actions/workflows/build.yml/badge.svg?branch=main
[actions-url]: https://github.com/cortexiumlabs/vectra/actions?workflow=build
[github-good-first-issue-badge]: https://img.shields.io/github/issues/cortexiumlabs/vectra/good%20first%20issue?style=flat-square&logo=github&label=good%20first%20issues
[github-good-first-issue-url]: https://github.com/cortexiumlabs/vectra/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22
[sonarcloud-quality-gate-badge]: https://sonarcloud.io/api/project_badges/measure?project=cortexiumlabs_vectra&metric=alert_status&token=0b5b5ca3c5f12401df0abb73c369c8a620fc174a
[sonarcloud-quality-gate-url]: https://sonarcloud.io/summary/new_code?id=cortexiumlabs_vectra
[sonarcloud-reliability-gate-badge]: https://sonarcloud.io/api/project_badges/measure?project=cortexiumlabs_vectra&metric=reliability_rating&token=0b5b5ca3c5f12401df0abb73c369c8a620fc174a
[sonarcloud-reliability-gate-url]: https://sonarcloud.io/summary/new_code?id=cortexiumlabs_vectra
[sonarcloud-maintainability-gate-badge]: https://sonarcloud.io/api/project_badges/measure?project=cortexiumlabs_vectra&metric=sqale_rating&token=0b5b5ca3c5f12401df0abb73c369c8a620fc174a
[sonarcloud-maintainability-gate-url]: https://sonarcloud.io/summary/new_code?id=cortexiumlabs_vectra
[sonarcloud-security-gate-badge]: https://sonarcloud.io/api/project_badges/measure?project=cortexiumlabs_vectra&metric=security_rating&token=0b5b5ca3c5f12401df0abb73c369c8a620fc174a
[sonarcloud-security-gate-url]: https://sonarcloud.io/summary/new_code?id=cortexiumlabs_vectra
[sonarcloud-vulnerabilities-gate-badge]: https://sonarcloud.io/api/project_badges/measure?project=cortexiumlabs_vectra&metric=vulnerabilities&token=0b5b5ca3c5f12401df0abb73c369c8a620fc174a
[sonarcloud-vulnerabilities-gate-url]: https://sonarcloud.io/summary/new_code?id=cortexiumlabs_vectra
[fossa-license-badge]: https://app.fossa.com/api/projects/git%2Bgithub.com%2Fcortexiumlabs%2Fvectra.svg?type=shield&issueType=license
[fossa-license-url]: https://app.fossa.com/projects/git%2Bgithub.com%2Fcortexiumlabs%2Fvectra?ref=badge_shield&issueType=license
[fossa-security-badge]: https://app.fossa.com/api/projects/git%2Bgithub.com%2Fcortexiumlabs%2Fvectra.svg?type=shield&issueType=security
[fossa-security-url]: https://app.fossa.com/projects/git%2Bgithub.com%2Fcortexiumlabs%2Fvectra?ref=badge_shield&issueType=security