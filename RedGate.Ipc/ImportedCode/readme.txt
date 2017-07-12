The code in this folder has been copy/pasted. Ideally, we would reference them via
Chris Lambrou's 'microlibraries', which are nuget packages that add file references
to the project rather than DLL dependencies, which this library does not want
because it itself will be a dependency and could cause diamonds of death.

Imported libraries
==================

rba100/DynamicProxy
rba100/TinyJsonSer (forked from ChrisLambrou/TinyJsonSer)