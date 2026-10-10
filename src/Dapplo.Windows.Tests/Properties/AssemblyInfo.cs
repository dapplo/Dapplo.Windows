// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// The tests use process wide resources (clipboard, hooks, the desktop, the foreground window): run them one at a time.
// xunit.v3 4.0 replaced CollectionBehavior(DisableTestParallelization, MaxParallelThreads) with Parallelization.
[assembly: Parallelization(Mode = ParallelMode.None, MaxThreads = 1)]