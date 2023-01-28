## Version: 0.1.7, 28 January 2023
----------------------------------
* Add `BWEM.NET.Benchmarks` project
* Add specific implementations of several utilities methods for `Position`, `WalkPosition`, `TilePosition`.
* Add specific implementations of `BreadthFirstSearch` for `TilePosition` and `WalkPosition`.
* Add Markable2D struct to encapsulate and reuse from `ArrayPool<bool>` the `visited` bool arrays from several methods.
* Optimize layout for several fields from `List` into arrays whenever possible.
* Use `stackalloc` for directional arrays instead of allocating on the heap.
* Use `Queue` instead of `List` wherever possible to make BFS iterations over tiles.
* Refactor `GetTile` calling code to store in variables before accessing `Tile` or `MiniTile` properties.
* Refactor sections of code into methods for easing of benchmarking.
* Fix in ChokePoint when computing _nodesInArea.
* Fix in Graph when computing chokepoint distances.
* Fix MarineHell when there are no chokepoints computed by BWEM.NET.
* Fix `Console.ReadLine` from example bots that prevents it to finish when the game is closed.

Performance improvement before and after these changes on map:

* Intel(R) Core(TM) i3-3120M CPU @ 2.50GHz
* 16 GB DDR3 RAM

(2)Astral Balance.scm
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 267.8 ms | 7.19 ms | 20.87 ms |   41.9 MB |
After:  | BWEMInit | 216.6 ms | 3.72 ms |  3.48 ms |  17.08 MB |

(2)Breaking Point.scx
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 257.6 ms | 10.21 ms| 29.77 ms |  41.67 MB |
After:  | BWEMInit | 214.2 ms | 3.61 ms |  3.37 ms |  16.99 MB |

(2)Isolation.scx
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 437.1 ms | 13.70 ms| 40.39 ms |  50.55 MB |
After:  | BWEMInit | 337.5 ms | 6.53 ms |  6.10 ms |  22.64 MB |

(2)Crystallis.scm
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 976.2 ms | 4.99 ms |  4.42 ms |   9.58 MB |
After:  | BWEMInit | 953.5 ms | 12.92 ms| 12.09 ms |   5.99 MB |

(3)Stepping Stones.scm
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 211.0 ms | 7.08 ms | 20.87 ms |   24.7 MB |
After:  | BWEMInit | 187.9 ms | 3.70 ms |  7.97 ms |   18.3 MB |

(4)Arctic Station.scx
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 960.2 ms | 12.59 ms| 11.16 ms |  176.9 MB |
After:  | BWEMInit | 859.1 ms | 7.84 ms |  6.55 ms |  49.26 MB |

(4)Space Debris.scm
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 291.7 ms | 8.99 ms | 26.21 ms |  53.29 MB |
After:  | BWEMInit | 868.1 ms | 17.13 ms| 22.87 ms |  53.83 MB |

(5)Twilight Star.scx
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 1.001 s | 0.0189 s | 0.0177 s | 255.75 MB |
After:  | BWEMInit | 877.2 ms | 17.27 ms | 23.63 ms | 53.83 MB |

(6)Sapphire Isles.scx
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 723.7 ms | 7.62 ms |  6.76 ms |  99.64 MB |
After:  | BWEMInit | 684.5 ms | 11.04 ms | 11.34 ms|  43.64 MB |

(7)Black Lotus.scx
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 725.2 ms | 13.90 ms| 13.65 ms | 210.66 MB |
After:  | BWEMInit | 598.4 ms | 8.96 ms |  8.38 ms |  47.68 MB |

(8)Frozen Sea.scx
        |   Method |     Mean |   Error |   StdDev | Allocated |
        |--------- |---------:|--------:|---------:|----------:|
Before: | BWEMInit | 2.362 s | 0.0548 s | 0.1615 s |   1.07 GB |
After:  | BWEMInit | 1.878 s | 0.0364 s | 0.0390 s | 137.91 MB |

