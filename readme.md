# Software-Security-Homework

An ISS project at Damascus University using AES, PGP and Digital Signatures to implement a secure messaging system with handshakes and session keys.

## Table of Contents

- [Language and Requirements](#language-and-requirements)
- [Structure](#language-and-requirements)
- [Running the project](#running-the-project)
- [License](#license)

## Language and Requirements

The project was made using C# as a .NET console app with the following packages:

- [Newtonsoft JSON.NET](https://www.newtonsoft.com/json)

- [Safester.CryptoLibrary](https://github.com/Safester-net/Safester.CryptoLibrary)

## Structure

There are 4 csprojects in the repo:

1. Server:
includes the driver class Program and server specific classes:
    - Connection: an instance that contains all of a connected client information
    - DBEntry a model to read/write to a file for storage
    - Server: the main class for all the server logic

2. Client:
includes the driver class Program and client  class:
    - Client: the main class for all the client logic

3. CA:
is a copy of Server with some specific functions

4. Common:
contains classes used by all other projects:
    - Coder: a class that handles all encoding and decoding using AES, PGP or non
    - Logger: for writing to cli in a clean format
    - Package: a model to convert a class to a  json string or a json string to a class
    - Utils: miscellaneous functions

## Running the project

use clients.sln to open the project as a VS solution, install the packages and then open a server and a client instances

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

Distributed under the MIT License. See `LICENSE` for more information
