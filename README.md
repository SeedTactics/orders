# SeedTactics Order Integration

This repository contains the order plugin API for [SeedTactics](https://www.seedtactics.com) and an example implementation of
the plugin API. This plugin is used to communicate orders between the ERP and the SeedTactics Orderlink software. For an overview
of this process, see the [whitepaper](https://www.seedtactics.com/docs/tactics/orders-erp-automation).

## Plugin API Source Code

[![NuGet Stats](https://img.shields.io/nuget/v/BlackMaple.SeedOrders.svg)](https://www.nuget.org/packages/BlackMaple.SeedOrders)

For documentation, first read the [whitepaper](https://www.seedtactics.com/docs/tactics/orders-erp-automation) for
an overview of how orders, bookings, and workorders fit into the entire SeedTactics ecosystem.
Then, see [Bookings.cs](src/BlackMaple.SeedOrders/Bookings.cs)
and [Workorders.cs](src/BlackMaple.SeedOrders/Workorders.cs) and [OrderInterface.cs](src/BlackMaple.SeedOrders/OrderInterface.cs).
The comments in these files describe in detail the API.

A plugin is any executable which reads the `BlackMaple.SeedOrders.OrderRequest`
formatted as JSON on standard input and writes formatted
JSON on standard output. If using C#, the [BlackMaple.SeedOrders](https://www.nuget.org/packages/BlackMaple.SeedOrders) NuGet
can be used. Other languages could also be used as long as the JSON formats are correctly serialized/deserialized.

## CSV Implementation

[![NuGet Stats](https://img.shields.io/nuget/v/BlackMaple.CSVOrders.svg)](https://www.nuget.org/packages/BlackMaple.CSVOrders)

The `src/BlackMaple.CSVOrders` directory contains an implementation of the plugin which uses CSV files, and is included
by default inside the SeedTactic software. You can examine the code of the CSV implementation for inspriation in implementing
an interface.
