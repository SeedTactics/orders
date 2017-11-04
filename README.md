# SeedTactics Order Integration

This repository contains the order plugin API for [SeedTactics](https://www.seedtactics.com) and an example implementation of
the plugin API.  This plugin is used to communicate orders between the ERP and the SeedTactics software.  For an overview
of this process, see the [whitepaper](https://www.seedtactics.com/guide/orders-erp-automation).

## Plugin API Source Code

The `src` directory contains the source code for the order plugin API plus the default CSV implementation.
If you are implementing a plugin, you don't need this source code and should instead reference the assemblies from
NuGet: [BlackMaple.SeedOrders](https://www.nuget.org/packages/BlackMaple.SeedOrders/) and
[BlackMaple.CSVOrders](https://www.nuget.org/packages/BlackMaple.CSVOrders/).

## Example Database Implementation

The `example-order-integration` directory contains an example implementation of the order plugin API using
EntityFramework and SQLite.  We suggest that if you are implementing a new plugin, you start by copying
and modifying the `example-order-integration` directory.

To implement a plugin, reference the [BlackMaple.SeedOrders](https://www.nuget.org/packages/BlackMaple.SeedOrders/)
assembly from NuGet and implement the `IBookingDatabase` and `IWorkorderDatabase` interfaces in a class with
a default no-parameter constructor.  When loading the plugin, the SeedTactics software searches all types in the
plugin DLL and creates an instance using the default no-parameter constructor.

For documentation, first read the [whitepaper](https://www.seedtactics.com/guide/orders-erp-automation) for
an overview of how orders, bookings, and workorders fit into the entire SeedTactics ecosystem.
Then, see [Bookings.cs](https://bitbucket.org/blackmaple/seedorders/src/tip/src/BlackMaple.SeedOrders/Bookings.cs)
and [Workorders.cs](https://bitbucket.org/blackmaple/seedorders/src/tip/src/BlackMaple.SeedOrders/Workorders.cs).  The comments
in these files describe in detail the API.  In particular, the API is mainly concerned just with copying and accessing data in
the ERP; no complex calculations are needed.
