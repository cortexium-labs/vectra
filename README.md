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

## Table of Contents

- [Overview](#overview-vectra)
  - [Key Capabilities](#key-capabilities)
  - [Why VECTRA?](#why-vectra%3F)
- [Key Features](#key-features)
- [Architecture](#architecture)
- [Quick Start](#quick-start)
  - [Run via Docker](#run-via-docker)
  - [Use a Pre-Built Binary](#use-a-pre-built-binary)
  - [Build from Source](#build-from-source)
- [Security](#security)
  - [Reporting a Vulnerability](#reporting-a-vulnerability)
  - [Security Policy](#security-policy)
  - [Dependency Scanning](#dependency-scanning)
- [Community & Contributing](#community-%26-contributing)
  - [Ways to Get Involved](#ways-to-get-involved)
  - [Contributing Code](#contributing-code)
- [License](#license)
- [Support Vectra](#support-vectra)

## Overview: VECTRA

> 📖 For full documentation, visit **[docs.cortexium.dev/vectra](https://docs.cortexium.dev/vectra)**.

**VECTRA**

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

## Architecture

![VECTRA Architecture](/img/architecture.jpg)

Every inbound HTTP request from an AI Agent flows through three layers inside the **Vectra Gateway**:

1. **Request Validation** — checks the API version header, authenticates the caller via JWT, and enforces rate limits. Failures are blocked immediately and recorded in the audit log.
2. **Decision Engine** — valid requests are evaluated by three sequential steps:
   - **Policy Evaluation** — applies configured rules and contextual conditions.
   - **Risk Scoring** — weighs contextual factors including request body, path, anomaly signals, and historical behaviour.
   - **Semantic Analysis** — classifies the underlying intent of the request.
3. **Routing outcome** — based on the decision engine result, the request is one of:
   - ✅ **Direct Allow** → forwarded to the upstream service via the proxy, audit recorded.
   - ⏳ **Pending Review** → held in the **HITL Review** queue for human approval. Approved requests are proxied; disapproved requests are blocked and audited.
   - 🚫 **Policy Block** → blocked immediately, audit recorded.

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

## Security

Security is a first-class concern in VECTRA. We follow responsible disclosure practices and take all reports seriously.

### Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

If you discover a security vulnerability, report it privately through one of the following channels:

* **GitHub Private Vulnerability Reporting:** Use the [Report a vulnerability](https://github.com/cortexiumlabs/vectra/security/advisories/new) button in the **Security** tab of the repository.
* **Email:** Send details to [contact@cortexiumlabs.com](mailto:contact@cortexiumlabs.com) with the subject line `[VECTRA] Security Vulnerability`.

Please include:
- A description of the vulnerability and its potential impact.
- Steps to reproduce or a proof-of-concept.
- Any relevant environment details (OS, .NET version, Docker image tag, etc.).

We aim to acknowledge reports within **48 hours** and provide a remediation timeline within **7 days**.

### Security Policy

The full security policy, including supported versions and disclosure process, is available in [SECURITY.md](https://github.com/cortexiumlabs/vectra/blob/main/SECURITY.md).

### Dependency Scanning

VECTRA uses [FOSSA](https://fossa.com) for continuous license and security scanning of all dependencies, and [SonarCloud](https://sonarcloud.io) for static analysis. Badge statuses are shown at the top of this file.

## Community & Contributing

VECTRA is built in the open and welcomes contributions of all kinds — bug reports, feature requests, documentation improvements, and code.

### Ways to Get Involved

* 🐛 **Report a bug** — [Open an issue](https://github.com/cortexiumlabs/vectra/issues/new?template=bug_report.md) with steps to reproduce and expected vs. actual behaviour.
* 💡 **Request a feature** — [Open a feature request issue](https://github.com/cortexiumlabs/vectra/issues/new?template=feature_request.md).
* 🔍 **Pick up a good first issue** — Browse issues labelled [good first issue](https://github.com/cortexiumlabs/vectra/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22) for beginner-friendly starting points.
* 📖 **Improve the docs** — Spotted something unclear or missing? PRs to docs are always welcome.

### Contributing Code

1. **Fork** the repository and create a feature branch from `main`.
2. **Write tests** for any new behaviour — the project uses `dotnet test`.
3. **Follow** the existing code style and conventions in the codebase.
4. **Open a Pull Request** against `main` with a clear description of what changed and why.

Please read [CONTRIBUTING.md](https://github.com/cortexiumlabs/vectra/blob/main/CONTRIBUTING.md) for the full contribution guidelines, code of conduct, and PR checklist before submitting.

### Community

* 🔔 Watch the repository to stay up to date with releases and RFC discussions.

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