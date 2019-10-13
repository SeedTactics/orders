# SeedTactics Order Integration

This repository contains the order plugin API for [SeedTactics](https://www.seedtactics.com) and an example implementation of
the plugin API. This plugin is used to communicate orders between the ERP and the SeedTactics Orderlink software. For an overview
of this process, see the [whitepaper](https://www.seedtactics.com/docs/concepts/orders-erp-automation).

## Plugin API Source Code

[![NuGet Stats](https://img.shields.io/nuget/v/BlackMaple.SeedOrders.svg)](https://www.nuget.org/packages/BlackMaple.SeedOrders)

The `src\BlackMaple.SeedOrders` directory contains the source code for the order plugin API.
To implement a plugin, reference the [BlackMaple.SeedOrders](https://www.nuget.org/packages/BlackMaple.SeedOrders/)
assembly from NuGet and implement the `IBookingDatabase` and `IWorkorderDatabase` interfaces in a class with
a default no-parameter constructor. When loading the plugin, the SeedTactics software searches all types in the
plugin DLL and creates an instance using the default no-parameter constructor.

For documentation, first read the [whitepaper](https://www.seedtactics.com/docs/concepts/orders-erp-automation) for
an overview of how orders, bookings, and workorders fit into the entire SeedTactics ecosystem.
Then, see [Bookings.cs](src/BlackMaple.SeedOrders/Bookings.cs)
and [Workorders.cs](src/BlackMaple.SeedOrders/Workorders.cs). The comments
in these files describe in detail the API. In particular, the API is mainly concerned just with copying and accessing data in
the ERP; no complex calculations are needed.

## CSV Implementation

[![NuGet Stats](https://img.shields.io/nuget/v/BlackMaple.CSVOrders.svg)](https://www.nuget.org/packages/BlackMaple.CSVOrders)

The `src/BlackMaple.CSVOrders` directory contains an implementation of the plugin which uses CSV files, and is included
by default inside the SeedTactic software. You can examine the code of the CSV implementation for inspriation in implementing
an interface.

## Example Database Implementation

The `example-order-integration` directory contains an example implementation of the order plugin API using
EntityFramework and SQLite. We suggest that if you are implementing a new plugin, you start by copying
and modifying the `example-order-integration` directory.
