# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.4.0] - 2026-09-08

### Added

- XCONS03: warn when a `[XamlConstructor]`-attributed type has no private or
  protected readonly fields without an initializer, so the generated
  constructor would be empty.
- XCONS04: warn when a `[XamlConstructor]`-attributed type is a nested type.
  Previously the generator silently emitted a non-compiling
  `partial class Outer.Inner` declaration for these instead of refusing to
  generate.
- XCONS05: warn when a `[XamlConstructor]`-attributed type's name doesn't end
  with "ViewModel". Previously the generator silently skipped these with no
  diagnostic at all.
- XCONS06: warn when a `[XamlConstructor]`-attributed type already declares a
  parameterless constructor.

### Fixed

- Nested types no longer produce invalid generated code - XCONS04 now
  reports the problem and skips generation instead.
