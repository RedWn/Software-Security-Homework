# Software-Security-Homework

An ISS project at Damascus University using AES, PGP and Digital Signatures to implement a secure messaging system with handshakes and session keys.

## Table of Contents

-   [Language and Requirements](#language-and-requirements)
-   [Structure](#structure)
-   [Running the project](#running-the-project)
-   [License](#license)
-   [Contributors](#contributors)

## Language and Requirements

The project was made using C# as a .NET console app with the following packages:

-   [Newtonsoft JSON.NET](https://www.newtonsoft.com/json)

-   [Safester.CryptoLibrary](https://github.com/Safester-net/Safester.CryptoLibrary)

## Structure

There are 4 csprojects in the repo:

1. Server:
   includes the driver class Program and server specific classes: - Connection: an instance that contains all of a connected client information - DBEntry a model to read/write to a file for storage - Server: the main class for all the server logic

2. Client:
   includes the driver class Program and client class: - Client: the main class for all the client logic

3. CA:
   is a copy of Server with some specific functions

4. Common:
   contains classes used by all other projects: - Coder: a class that handles all encoding and decoding using AES, PGP or non - Logger: for writing to cli in a clean format - Package: a model to convert a class to a json string or a json string to a class - Utils: miscellaneous functions

## Running the project

use clients.sln to open the project as a VS solution, install the packages and then open a server and a client instances

## License

Distributed under the MIT License. See `LICENSE` for more information

## Contributors

-   [Redwan Alloush](https://github.com/RedWn)
-   [Hasan Mothaffar](https://github.com/HasanMothaffar)
-   [Iyad Al-Ansary](https://github.com/IyadAlanssary)
-   [Tarek Al-Habbal](https://github.com/tarook0)
