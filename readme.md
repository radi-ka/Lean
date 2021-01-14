# Testing Overload
__powered by [lean](https://github.com/QuantConnect/Lean)__

This repo contains the files used for the blog series "Testing Overload".
We develop a trend trading forex bot by leveraging the [no nonsense forex - NNFX](https://nononsenseforex.com/) trading system proposal.

## Setup
0. prerequisites
    - `git`
    - `docker`
    - [QuantConnect](https://www.quantconnect.com/) account (for data access. Alternatively provide your own)
1. clone this repo
2. build the docker image `docker build --tag lean-rider --file Dockerfile_dev_env .`
3. start rider `./rider.sh`

## Development
1. have a look at `Algorithm.CSharp/TestingOverload/NnfxAlgorithm.cs`
2. because we will run lean in a separate container we can unload everything except
   - QuantConnect
   - QuantConnect.Algorithm
   - QuantConnect.Algorithm.CSharp
   - QuantConnect.Algorithm.Framework
   - QuantConnect.Configuration
   - QuantConnect.Indicators
   - QuantConnect.Logging
3. build the solution

## Backtesting
1. have a look at `config.json`
2. start the backtest by `./run_backtest.sh` while providing the necessary env variables (this is easily done by using riders `Shell Script` plugin and configuration template)
3. wait. The first run will take some time as data needs to be downloaded.

---

original readme can be found [here](lean_readme.md)