# Project: Dominus Bellum - "Lord of War"

> Design document by Trent VanSlyke 
>
> Initial draft: 12/27/2020 - Last update: 12/27/2020
> 
> Reviewers: N/A

# Overview

Terra Bellum is a project to create a Dawn of War I clone using a modern graphics engine. The project is to be designed with extreme flexibility in mind in terms of content. The plan is to allow complex mods to provide the majority of the content, ranging from Warhammer massive scale to World War 2 battles to squad-level tactics.

### Ideal final product

When you download the game, you are provided with a set of sufficient quality content to complete as both a single-player campaign (playtime: 10hr), and a quality multiplayer skirmish or campaign coop experience. After the completion of this core content, you should be able to download a mod which will act as a total conversion to a different setting. These conversions will include new models, new campaigns, new units, new factions, new resources, and new levels. For example: after completing an Age of Empires clone mod, you can go to the main menu and switch to a Warhammer 40k mod to play skirmish and coop with friends.

Modding the game should be a simple process. Factions, units, buildings, map elements, resources, and missions should be structured such that they can be created in a text editor. Downloading a mod should be a simple zip and drag-n-drop install. The game should function equally well with 5 units as 500 units, with settings from Age of Empires to Ultimate Apocalypse. 

### Roadmap

The project should be completed following the task list relatively closely. The game must run and display units appropriately prior to gameplay being developed, and gameplay must be functional before it can be data-driven.

The project does not have set deadlines or demands. However, the following task list must be completed to have a minimum viable product.

In order of importance:

1. Minimum viable product (required!)
   * See: task list
2. Stability
   * No crashes to desktop, game breaking bugs, or online desyncronizations
4. Performance 
   * Render budget consuming no more than 10ms in extremis, or 100fps, as measured by an end-game battle on developers machine. The target is under 5ms render time while showing at least 500 units on a large map.
4. Content 
   * Placeholder/example content, followed by developer and community mods
5. High quality code
   * Should be extremely readable, extensively documented, unsurprising, easy to hunt through, and displaying common sense over formal correctness

### Task List:

This task list is **incomplete** and used merely as a tracker that can be expanded as required.

- [ ] Minimum viable product (functional game)
  - [ ] Display
    - [ ] Simple unit
    - [ ] Unit with lighting
    - [ ] Squad of multiple of same unit
    - [ ] Different kinds of unit at same time
    - [ ] Level display
    - [ ] Interface
      - [ ] Simple overlay
      - [ ] Tracking faction-level resources
      - [ ] Tracking selected unit/building with active effects, resources
      - [ ] Minimap
      - [ ] Widgets (ping, select all, automate)
      - [ ] Menus
        - [ ] Main menu
        - [ ] Settings
        - [ ] Skirmish select
        - [ ] Multiplayer lobby host/join
        - [ ] Mod select (core/40k/ww2/starcraft/warcraft/xcom)
        - [ ] Army painter
  - [ ] Gameplay
    - [ ] Moving units
      - [ ] Pathfinding
      - [ ] Units with resources (health/morale/ammo)
      - [ ] Units attacking other units
    - [ ] Buildings
      - [ ] Scripted effects (resource production)
      - [ ] Active effects (unit production)
      - [ ] Defensive (attacks enemy units)
    - [ ] Animation handling
    - [ ] Netcode
    - [ ] Enemy AI
      - [ ] Simple movement and attack
      - [ ] Scouting
      - [ ] Analysis of enemy units
      - [ ] Cost/benefit analysis of counters
  - [ ] Data-driven design
    - All of these items should be able to be designated in text files
    - [ ] Level editor
    - [ ] Designing units and buildings
      - [ ] Importing models 
      - [ ] Statistics
    - [ ] Designing factions
      - [ ] Custom unit pools
      - [ ] Unique resources
    - [ ] Designing missions / preset items on map
    - [ ] Designing campaigns (series of missions)
