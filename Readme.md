##XafToXaml

XafToXaml is an application to convert .xaf animation files exported from 3ds Max into XAML animations, which can then be used in XAML Storyboards.

Currently, the application is tailored to suit my particular needs, one of which is to extract the Position and Rotation of a single animated object in a 3D scene. The other one is to accomodate the difference in coordinate axes in the .3DS file and the XAML scene I generate separately; for some reason, XAML scenes that my exporter application generates are rotated by minus 90 degrees around the x-axis.

Licensed under the Microsoft Public license.

---

####What are .xaf files?  
As far as this application is concerned, .xaf files are 3ds Max animations exported to XML format