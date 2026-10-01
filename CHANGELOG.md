# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="6.1.0"></a>
## [6.1.0](https://www.github.com/mu88/ShopAndEat/releases/tag/6.1.0) (2026-10-01)

### ✨ Features

* introduce AI-based shopping agent ([69d5391](https://www.github.com/mu88/ShopAndEat/commit/69d53914ef621ec66028bf0f52431b35e7cf6437))
* migrate code coverage from coverlet to dotnet-coverage ([b86d476](https://www.github.com/mu88/ShopAndEat/commit/b86d47646349d6d6d090b9a92c772366b2e4f37a))
* restructure application components and routing for improved organization ([f6b4bb1](https://www.github.com/mu88/ShopAndEat/commit/f6b4bb1a79d2b56d6c016f97f8a4bc12b3435cf9))
* **renovate:** add timestamp-optional behavior for specific packages to ensure updates are not skipped and remove AI artifact ([1fe7ea0](https://www.github.com/mu88/ShopAndEat/commit/1fe7ea03d2d55fff9aa07bdab5c1607e9a03c5f0))
* **shopping-agent:** add always_ask mechanism and remove hard-coded product selection defaults ([a6d6c17](https://www.github.com/mu88/ShopAndEat/commit/a6d6c17ceb79551e3f186f79da724f034d4f16dd))
* **shopping-agent:** fix cart bugs and add completeness verification ([7ca3b1b](https://www.github.com/mu88/ShopAndEat/commit/7ca3b1bd3efdf64b62c954fa8ce60b326d6a77b7))
* **shopping-agent:** implement two-phase shopping workflow ([9d9ee5f](https://www.github.com/mu88/ShopAndEat/commit/9d9ee5fced51372a5e4f670d14591b73899914e7))
* **shopping-agent:** improve token usage ([37d8f34](https://www.github.com/mu88/ShopAndEat/commit/37d8f346daf4032bc884ca034acd30be5ea268ff))
* **shopping-agent:** migrate from Blazor WASM to Blazor Server (InteractiveServer) ([8bd72a6](https://www.github.com/mu88/ShopAndEat/commit/8bd72a6749cd09d63672a619aa3004464e4cc1b1))

### 🐛 Bug Fixes

* only take future meals into account when calculating the shopping list ([bf0a3d8](https://www.github.com/mu88/ShopAndEat/commit/bf0a3d806b9db157882efa0c8c3ff2c3c29adaa6))
* **deps:** override vulnerable SQLite transitive dependency ([f89ffd5](https://www.github.com/mu88/ShopAndEat/commit/f89ffd5523afe8e6775c9cc76469d0efca549e17))
* **shopping-agent:** validate message origin in extension bridge ([8d5312b](https://www.github.com/mu88/ShopAndEat/commit/8d5312b53f044e556e9f0af43e35c6e007704aa8))
* **shopping-assistant:** allow HTTPS and fix scrolling issue ([22205c3](https://www.github.com/mu88/ShopAndEat/commit/22205c322e5d9540bc6d6fe22b98bee069ac929e))
* **tests:** use valid SemVer for system test container image tag ([79c6cd0](https://www.github.com/mu88/ShopAndEat/commit/79c6cd01ae8ffe971c08be0e6d96ab8ba46165f5))

### ♻️ Refactors

* achieve quality parity via domain modernization and AI-driven review fixes ([c0fdafc](https://www.github.com/mu88/ShopAndEat/commit/c0fdafcf7e73b2609c79dc00417de240beefc154))
* remove runtime mapping dependency ([f2592f3](https://www.github.com/mu88/ShopAndEat/commit/f2592f38266e1b9907dc665074585bd4355c1c5e))

### ✅ Tests

* add opt-in live LLM integration tests ([059b9bb](https://www.github.com/mu88/ShopAndEat/commit/059b9bbcb49e9097e7d21bb2c293ddfcea3a85b9))
* improve system test ([4e00e15](https://www.github.com/mu88/ShopAndEat/commit/4e00e15ed214e033b4b1b1ca668a0fa816ddcafd))
* simplify build process by using CliWrap ([342bbf5](https://www.github.com/mu88/ShopAndEat/commit/342bbf5e5dbd4ee2c428a7e28bed848245cdfd88))

### 🔧 Chores

* apply new code style ([b204e92](https://www.github.com/mu88/ShopAndEat/commit/b204e92dc6efb8ee1c76790370bc50d53f8120f0))
* resolve several issues related to previous commits ([b9b6331](https://www.github.com/mu88/ShopAndEat/commit/b9b633148232319257f80a2faca8f3d9d1da3662))
* update launchSettings.json for improved profile configuration ([4e46475](https://www.github.com/mu88/ShopAndEat/commit/4e46475e7d4d99db98d7f2f8a382b564972aa07e))
* use new shared GitHub repo mu88/common ([f8a4f0c](https://www.github.com/mu88/ShopAndEat/commit/f8a4f0c286feac38d77dbaf84e862b783a834f66))
* **deps:** install EF Core tools ([b87e563](https://www.github.com/mu88/ShopAndEat/commit/b87e563a146c0639b580c4a6176b94f7858afc7d))
* **deps:** resolve security vulnerability by upgrading `AutoMapper` ([1459be0](https://www.github.com/mu88/ShopAndEat/commit/1459be0a8f4aa9ee68a6a418adf6098ea7b01181))
* **deps:** update all dependencies ([d4fd729](https://www.github.com/mu88/ShopAndEat/commit/d4fd729cec8f6f19395a923519b85102106e755f))
* **deps:** update all dependencies ([45ae5ab](https://www.github.com/mu88/ShopAndEat/commit/45ae5abbf3c702b733885258abcd947d1cf74560))
* **deps:** update all dependencies ([97806da](https://www.github.com/mu88/ShopAndEat/commit/97806da1e6ee612c06cc72076b9fc8b5e8e12940))
* **deps:** update all dependencies ([3398223](https://www.github.com/mu88/ShopAndEat/commit/339822378a010a40b12fdd01e926c412617ffeec))
* **deps:** update all dependencies ([7917203](https://www.github.com/mu88/ShopAndEat/commit/791720385136c547e0817bef002bb101b75aecaf))
* **deps:** update all dependencies ([7d6ea34](https://www.github.com/mu88/ShopAndEat/commit/7d6ea34388eef696057032f5e9d3a1eba87cfca2))
* **deps:** update all dependencies ([c0379cb](https://www.github.com/mu88/ShopAndEat/commit/c0379cb25ad10443a793dca2a9e96ffe5a94b3c4))
* **deps:** update all dependencies ([13f5735](https://www.github.com/mu88/ShopAndEat/commit/13f5735004a8b382d5caee4e7543853e54f28024))
* **deps:** update all dependencies ([1abde86](https://www.github.com/mu88/ShopAndEat/commit/1abde86809c307f33cd9a2165349d8aac63316fe))
* **deps:** update all dependencies ([0bbe6f6](https://www.github.com/mu88/ShopAndEat/commit/0bbe6f6a5c0fdb516940cd43d2d982f4aca90bd9))
* **deps:** update all dependencies ([7dc80b5](https://www.github.com/mu88/ShopAndEat/commit/7dc80b56f527d053c1b7e0d7b0bbc58f67880098))
* **deps:** update all dependencies ([dd8b0a5](https://www.github.com/mu88/ShopAndEat/commit/dd8b0a50cff45767968beb90daa3186aea891a07))
* **deps:** update all dependencies ([8aba9c1](https://www.github.com/mu88/ShopAndEat/commit/8aba9c12b43652515d34ed8fba9329f4964f3b5e))
* **deps:** update all dependencies ([0090b8c](https://www.github.com/mu88/ShopAndEat/commit/0090b8c8eff9d0880cdf53b5d72aa8b3c95e0f89))
* **deps:** update all dependencies ([44547bd](https://www.github.com/mu88/ShopAndEat/commit/44547bdccf140dfd0ce1e14fbad63f7b31f6cb24))
* **deps:** update all dependencies ([ac0ae7d](https://www.github.com/mu88/ShopAndEat/commit/ac0ae7df448693923e6754a117d90ac13a283d14))
* **deps:** update all dependencies ([92ee7dd](https://www.github.com/mu88/ShopAndEat/commit/92ee7dd1d4a10fc64501ffdb3d17d3e1312827fb))
* **deps:** update all dependencies to 10.0.2 ([b8813f6](https://www.github.com/mu88/ShopAndEat/commit/b8813f600a782518464f3da4ee4fbda03a8a2e9f))
* **deps:** update all dependencies to 10.0.6 ([d9f5612](https://www.github.com/mu88/ShopAndEat/commit/d9f5612d761497709511a2fdb5d542b12c15ecb2))
* **deps:** update all dependencies to 3.10.2 ([e841617](https://www.github.com/mu88/ShopAndEat/commit/e841617ec93198b5c940834138a8fe53f49a9057))
* **deps:** update dependency microsoft.extensions.ai.openai to 10.5.0 ([ccc483f](https://www.github.com/mu88/ShopAndEat/commit/ccc483f65e5598b88d5a1b6e61994bdbf3e8ac40))
* **deps:** update dependency scalar.aspnetcore to 2.17.3 ([2594ba2](https://www.github.com/mu88/ShopAndEat/commit/2594ba25b940a2d1ce33f1feb5135a176f943147))
* **deps:** update mu88.Shared to version 6.0.0 for latest OTEL fixes ([022cf26](https://www.github.com/mu88/ShopAndEat/commit/022cf26a6ec82c3d083ce1bd8ffabec4f2536506))
* **deps:** update mu88.Shared to version 7.0.0 for latest OTEL fixes ([a8bc33e](https://www.github.com/mu88/ShopAndEat/commit/a8bc33efb9cdb692a24b8dbda6119500f5f1c34b))
* **deps:** update mu88/common digest to 03ce929 ([9851a70](https://www.github.com/mu88/ShopAndEat/commit/9851a70a105eaaa518deb7e075611a8100315fa7))
* **deps:** update mu88/common digest to 0d04e69 ([fe4961d](https://www.github.com/mu88/ShopAndEat/commit/fe4961d8d02e869171726dbcda0c6c9272236075))
* **deps:** update mu88/common digest to 349db23 ([8e962d2](https://www.github.com/mu88/ShopAndEat/commit/8e962d222d4ddbcc124ca68df968c11a4e7e094d))
* **deps:** update mu88/common digest to 5d4855b ([aec0e5d](https://www.github.com/mu88/ShopAndEat/commit/aec0e5d9241f710f9823b550acc1d7cff7e81039))
* **deps:** update mu88/common digest to 88212c6 ([4c82e32](https://www.github.com/mu88/ShopAndEat/commit/4c82e32af186737f466dbf08d8811a6d62703375))
* **deps:** update mu88/common digest to 8aa4af2 ([be5d098](https://www.github.com/mu88/ShopAndEat/commit/be5d09851b18c99cd83c618b12a4b4799734557b))
* **deps:** upgrade mu88.Shared for latest OTel / health check updates ([7d035fe](https://www.github.com/mu88/ShopAndEat/commit/7d035fe6fc37f93dcae83e88824d1910bfac2b12))
* **deps:** use LibMan ([46d5d49](https://www.github.com/mu88/ShopAndEat/commit/46d5d497be171b82cc5df901a83e21a215af858c))
* **dev:** fix broken DotSettings links ([7b9197a](https://www.github.com/mu88/ShopAndEat/commit/7b9197a3a83d842af611303b88de26c6d0d99c9d))

<a name="6.0.0"></a>
## [6.0.0](https://www.github.com/mu88/ShopAndEat/releases/tag/v6.0.0) (2025-12-12)

### ✨ Features

* upgrade to .NET 10 ([4414a62](https://www.github.com/mu88/ShopAndEat/commit/4414a62bbe0055c5b0866d76c4e3fa2587888a42))

### ♻️ Refactors

* replace Swashbuckle with Microsoft's OpenAPI package and use Scalar for UI ([42a8a63](https://www.github.com/mu88/ShopAndEat/commit/42a8a639d57ea0339804c8cf043970137525047e))

### 🔧 Chores

* switch to new SLNX format ([bd11cc3](https://www.github.com/mu88/ShopAndEat/commit/bd11cc33d2d9c8bb3c376defc7ebe7ad1be3db17))
* **deps:** update all dependencies ([19350da](https://www.github.com/mu88/ShopAndEat/commit/19350dae14cc4d4027c17c4c559bec9ef2526cc7))
* **deps:** update all dependencies ([edeedd9](https://www.github.com/mu88/ShopAndEat/commit/edeedd9fa38c0b952c375c7139c53715d134971c))
* **deps:** update all dependencies ([e75f446](https://www.github.com/mu88/ShopAndEat/commit/e75f44663c45561d3dc8e583eae20e247f069f1d))

### Breaking Changes

* upgrade to .NET 10 ([4414a62](https://www.github.com/mu88/ShopAndEat/commit/4414a62bbe0055c5b0866d76c4e3fa2587888a42))

<a name="5.0.0"></a>
## [5.0.0](https://www.github.com/mu88/ShopAndEat/releases/tag/v5.0.0) (2025-10-05)

### Bug Fixes

* dynamically determine user's home directory by using the corresponding environment variable ([0604c0a](https://www.github.com/mu88/ShopAndEat/commit/0604c0a6dbc8b6d5bba292fa6404958aa20bd4c0))

### Breaking Changes

* use GitHub Container Registry instead of Docker Hub ([bc6a28b](https://www.github.com/mu88/ShopAndEat/commit/bc6a28b8838926e27e7bed26dd94d263c29ed0b0))

<a name="4.1.0"></a>
## [4.1.0](https://www.github.com/mu88/ShopAndEat/releases/tag/v4.1.0) (2024-12-20)

### Features

* add health check ([6cf53ff](https://www.github.com/mu88/ShopAndEat/commit/6cf53ff127b940e2ed90346f92bbb321dd4ee2d7))
* add reusable workflow ([ab03201](https://www.github.com/mu88/ShopAndEat/commit/ab032017ccabee03aa091bd85b5077ac0f8d32de))
* embed health check tool ([16b5eca](https://www.github.com/mu88/ShopAndEat/commit/16b5ecadc1fdcf9ee38e1e95bf155b895906dce8))

<a name="4.0.0"></a>
## [4.0.0](https://www.github.com/mu88/ShopAndEat/releases/tag/v4.0.0) (2024-12-06)

### Features

* **deps:** upgrade to .NET 9 ([485c61d](https://www.github.com/mu88/ShopAndEat/commit/485c61d83ff197aa080d237788111597c082fa2b))

### Breaking Changes

* **deps:** upgrade to .NET 9 ([485c61d](https://www.github.com/mu88/ShopAndEat/commit/485c61d83ff197aa080d237788111597c082fa2b))

<a name="3.2.1"></a>
## [3.2.1](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.2.1) (2024-10-05)

### Bug Fixes

* don't report validation error when adding meal for today ([c9e0b66](https://www.github.com/mu88/ShopAndEat/commit/c9e0b66e64700125d4b54e4d016fa3a14fae26d3))

<a name="3.2.0"></a>
## [3.2.0](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.2.0) (2024-08-03)

### Features

* replace OpenTelemetry and multi-manifest Docker image logic with NuGet package mu88.Shared ([e834aa9](https://www.github.com/mu88/ShopAndEat/commit/e834aa9da92db88dff639f1518e6630d26226e4b))

<a name="3.1.1"></a>
## [3.1.1](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.1.1) (2024-06-29)

<a name="3.1.0"></a>
## [3.1.0](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.1.0) (2024-06-16)

### Features

* add OpenTelemetry ([9d9cd1e](https://www.github.com/mu88/ShopAndEat/commit/9d9cd1e9f3eb533879d24477b629581008e33eda))

<a name="3.0.4"></a>
## [3.0.4](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.0.4) (2023-12-01)

<a name="3.0.3"></a>
## [3.0.3](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.0.3) (2023-11-17)

<a name="3.0.2"></a>
## [3.0.2](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.0.2) (2023-11-17)

<a name="3.0.1"></a>
## [3.0.1](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.0.1) (2023-11-17)

<a name="3.0.0"></a>
## [3.0.0](https://www.github.com/mu88/ShopAndEat/releases/tag/v3.0.0) (2023-11-17)

### Features

* use .NET 8 ([6e2c80e](https://www.github.com/mu88/ShopAndEat/commit/6e2c80e14a04784468764a445e0b37796449b5c5))

### Breaking Changes

* use .NET 8 ([6e2c80e](https://www.github.com/mu88/ShopAndEat/commit/6e2c80e14a04784468764a445e0b37796449b5c5))

<a name="2.14.0"></a>
## [2.14.0](https://www.github.com/mu88/ShopAndEat/releases/tag/v2.14.0) (2023-11-17)

### Features

* control number of days ([58a277d](https://www.github.com/mu88/ShopAndEat/commit/58a277d890a6e5edf6b6a0cc2aa427c2e9922e92))
* control number of persons ([f257e21](https://www.github.com/mu88/ShopAndEat/commit/f257e2184acc9dfd4b89cf6dfe117751a7524d78))
* provide today's meals via HTTP API ([bebaa0b](https://www.github.com/mu88/ShopAndEat/commit/bebaa0b19b23e87d837c3c6c7b44895fedd341d7))
* scroll to EditForm on editing ([26c0bdc](https://www.github.com/mu88/ShopAndEat/commit/26c0bdc8559d36af00a3a783fb82928d98a5966d))
* show confirmation after saving entity ([a6e24b8](https://www.github.com/mu88/ShopAndEat/commit/a6e24b83cd22c030c8933d88cda06d77f63eee87))
* show number of persons for multi-day meals ([d2c00b6](https://www.github.com/mu88/ShopAndEat/commit/d2c00b6bedff01c4de001360facb52026bf78872))
* toggle meal ([6a6fe3f](https://www.github.com/mu88/ShopAndEat/commit/6a6fe3f5fb1539e50f99da0f8b8ebdd6051518ff))
* use EF Core migrations ([7a039f8](https://www.github.com/mu88/ShopAndEat/commit/7a039f85896518060eacf5078cee7079252bbeed))

### Bug Fixes

* don't reset dropdowns after saving a meal ([9afd566](https://www.github.com/mu88/ShopAndEat/commit/9afd56627b5c43093f0b6c8886000045d22c67f1))
* overwrite certificate to fix "file already exists" issue on startup ([1a7dba1](https://www.github.com/mu88/ShopAndEat/commit/1a7dba13d39e2119e0e77987e89060d6cc548451))
* show meals for today in app overview ([d7e1ca1](https://www.github.com/mu88/ShopAndEat/commit/d7e1ca13df5ddbe4ba79c4092c55caa848c569c4))
* use correct DB path ([f8e9d36](https://www.github.com/mu88/ShopAndEat/commit/f8e9d3609a8d269ae8b89e041c9ac428d020cff0))
* use rational person quantifier ([3c49fa2](https://www.github.com/mu88/ShopAndEat/commit/3c49fa2164970b6b37183b292852012ef78e7401))

