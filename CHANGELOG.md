## Version: 0.1.7, 25 January 2023
----------------------------------
* Add `BWEM.NET.Benchmarks` project
* Remove `Console.ReadLine` from example bots that prevents it to finish when the game is closed.
* Add specific implementations of several utilities methods for `Position`, `WalkPosition`, `TilePosition`.
* Change internal `tiles` and `walkTiles` from `List` to arrays.
* Add specific implementations of `BreadthFirstSearch` for `TilePosition` and `WalkPosition`.
* Use `stackalloc` for directional arrays instead of allocating on the heap.
* Refactor `GetTile` calling code to store in variables before accessing `Tile` or `MiniTile` properties.
* Use `Queue` instead of `List` wherever possible to make BFS iterations over tiles.
* Fix in ChokePoint when computing _nodesInArea.
* Fix in Graph when computing chokepoint distances.
* Fix MarineHell when there are no chokepoints computed by BWEM.NET.

Performance improvement before and after these changes on map:

* Intel(R) Core(TM) i3-3120M CPU @ 2.50GHz
* 16 GB DDR3 RAM

(8)Frozen Sea.scx
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before:   Out of Memory Exception
After:  | BWEMInit | 2.362 s | 0.0548 s | 0.1615 s |   1.07 GB |

