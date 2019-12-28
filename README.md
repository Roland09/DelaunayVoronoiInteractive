# Interactive Delaunay Triangulation & Voronoi Diagram

This repository contains a fork of the work of Rafael Kuebler. I extended it with a little bit of interaction.

What you see in this video is the dependency of Points which are triangulated using Delaunay Triangulation. The dual graph of it can be created, i. e. the Voronoi Diagram.

In order to show how it's all interconnected I added visualization features like

* Points
* Circumcenters
* Circumcircles

The visualization graphs are

* Delaunay Triangulation
* Voronoi Diagram

The Points can be placed individually or continuously. The continuous mode recreates the graph on mouse move, so that's rather performance intense.

You can activate the Voronoi Diagram filling either on the currently moved Point or on the entire Diagram.

Convenience functions can be invoked on button click:

* Clear the canvas
* Add a specified number of random Points
* Add individual patterns like horizontal points, vertical points, diagonal points, cross shaped points, points distributed along circle and ellipse

Here's a quick overview video:

[![Overview](http://img.youtube.com/vi/pCoxJM4RHEY/0.jpg)](https://www.youtube.com/watch?v=pCoxJM4RHEY)

## Other References

If you are interested in more like this, here are some recommendations:

- Habrador 

  https://www.habrador.com/tutorials/math/13-voronoi/

- Paul Bourke

  http://paulbourke.net/papers/triangulate/

## Credits
Credits & many thanks to Rafael Kuebler for providing his implementation for free and with the MIT license. You can find his work here:

https://github.com/RafaelKuebler/DelaunayVoronoi


