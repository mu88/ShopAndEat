# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

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

