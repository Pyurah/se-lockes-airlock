# Credits

## Original Work

**Locke's Airlocks** is a modernized derivative of
**Blargmode's Aggressive Airlocks** (version 6.4, 2020-08-27).

- **Original author:** Blargmode
- **Original script:** [Aggressive Airlocks on the Space Engineers Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=1219622614)
- **Derived from:** `original/aggressive-airlocks.txt` (archived in this repository)

The original Aggressive Airlocks script provided the complete feature set, airlock state-machine
logic, atmosphere-mode detection, and the block-scanning algorithm that this project is built upon.

## What changed in this derivative

- Complete code rewrite into a clean, readable multi-file MDK2 project structure
- DLC-aware block detection for blocks released after 2020:
  Small Oxygen Tank, Lab Oxygen Tank (Fieldwork), Prototech O2/H2 Generator (Prosperity),
  Small Gate Tall/Wide (Contact), and future blocks via keyword lists
- Renamed from "Aggressive Airlocks" to "Locke's Airlocks"
- Added unit test suite covering settings, tag matching, text utilities, and block classification

## License acknowledgement

This project is released under the MIT License. The original Aggressive Airlocks script was
published on the Steam Workshop under Workshop terms. This derivative is a community project
and is not affiliated with, endorsed by, or supported by the original author.

If you are Blargmode and have concerns about this derivative, please open an issue or contact
the repository owner.
