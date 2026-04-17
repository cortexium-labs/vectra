<div align="center">
  <img src="/img/banner.png" alt="Vectra Banner" />
  <h2>VECTRA — Intent-Aware Governance Gateway</h2>

  [![dotnet][dotnet-budge]][dotnet-url]
  [![Quality Gate Status][sonarcloud-quality-gate-badge]][sonarcloud-quality-gate-url]
  [![License: Apache 2.0][apache-badge]][apache-url]
  [![Build Status][actions-badge]][actions-url]
  [![FOSSA License Status][fossa-license-badge]][fossa-license-url]
  [![FOSSA License Status][fossa-license-badge]][fossa-license-url]
  [![FOSSA Security Status][fossa-security-badge]][fossa-security-url]
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

## License

Vectra is open-source and licensed under the **Apache 2.0 License**.  
See [LICENSE](https://github.com/cortexium-labs/vectra/blob/main/LICENSE) for details.

## Support Vectra
[![⭐ Star on GitHub](https://img.shields.io/badge/⭐%20Star%20on%20GitHub-555555?style=flat&logo=github)](https://github.com/cortexium-labs/vectra)  
✨ **Support Vectra by giving it a star!** ✨  
Your support helps others discover the project and drives continued innovation.

[dotnet-budge]: https://img.shields.io/badge/.NET-10.0-green
[dotnet-url]: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
[apache-badge]: https://img.shields.io/badge/License-Apache%202.0-blue.svg?style=flat&logo=github
[apache-url]: https://opensource.org/licenses/Apache-2.0
[actions-badge]: https://github.com/cortexium-labs/vectra/actions/workflows/build.yml/badge.svg?branch=main
[actions-url]: https://github.com/cortexium-labs/vectra/actions?workflow=build
[github-good-first-issue-badge]: https://img.shields.io/github/issues/cortexium-labs/vectra/good%20first%20issue?style=flat-square&logo=github&label=good%20first%20issues
[github-good-first-issue-url]: https://github.com/cortexium-labs/vectra/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22
[sonarcloud-quality-gate-badge]: https://sonarcloud.io/api/project_badges/measure?project=cortexium-labs_vectra&metric=alert_status
[sonarcloud-quality-gate-url]: https://sonarcloud.io/summary/new_code?id=cortexium-labs_vectra
[fossa-license-badge]: https://app.fossa.com/api/projects/git%2Bgithub.com%2Fcortexium-labs%2Fvectra.svg?type=shield&issueType=license
[fossa-license-url]: https://app.fossa.com/projects/git%2Bgithub.com%2Fcortexium-labs%2Fvectra?ref=badge_shield&issueType=license
[fossa-security-badge]: https://app.fossa.com/api/projects/git%2Bgithub.com%2Fcortexium-labs%2Fvectra.svg?type=shield&issueType=security
[fossa-security-url]: https://app.fossa.com/projects/git%2Bgithub.com%2Fcortexium-labs%2Fvectra?ref=badge_shield&issueType=security