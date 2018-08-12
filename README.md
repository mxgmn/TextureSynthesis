<p align="center"><img src="http://i.imgur.com/fdb7B52.png"></p>

The algorithms are:

1. Full neighbourhood search algorithm of [Scott Draves](http://draves.org/fuse) and [Alexei Efros + Thomas Leung](https://www.eecs.berkeley.edu/Research/Projects/CS/vision/papers/efros-iccv99.pdf) and [Li-Yi Wei + Marc Levoy](https://graphics.stanford.edu/papers/texture-synthesis-sig00/texture.pdf) is probably the simplest texture synthesis algorithm imaginable.
2. K-coherent neighbourhood search of [Michael Ashikhmin](http://www.cs.princeton.edu/courses/archive/fall10/cos526/papers/ashikhmin01a.pdf) and [Xin Tong + Jingdan Zhangz + Ligang Liu + Xi Wangz + Baining Guo + Heung-Yeung Shum](http://research.microsoft.com/pubs/65191/btfsynthesis.pdf)  takes computational burden from the synthesis to the analysis part and therefore is better suited for synthesizing large textures.
3. Resynthesis algorithm of [P. F. Harrison](http://logarithmic.net/pfh-files/thesis/dissertation.pdf) is  scale-invariant, fast, supports constraints and practically never produces completely unsatisfactory results.

Note that my implementations are not completely true to the original papers.

Watch a video demonstration of P. F. Harrison's algorithm on YouTube: [https://www.youtube.com/watch?v=8sUMBMpZNzk](https://www.youtube.com/watch?v=8sUMBMpZNzk).

<p align="center"><img src="http://i.imgur.com/jO7YzUY.gif"></p>
<p align="center">
	<img src="http://i.imgur.com/E5prVhn.png">
	<img src="http://i.imgur.com/UJnnryW.png">
</p>

<h2>Building</h2>

1. Download this repository and extract it in a folder named "SynTex-master" (the instructions below depends on that name).
2. Download and install `Microsoft .NET Framework`, once you have it installed, check if `dotnet` command can be called from a terminal (console) before continuing.

3. From the command line, access the extracted project folder, create the project file and require System.Drawing like this:

````batch
cd SynTex-master
dotnet new console
dotnet add package System.Drawing.Common --version 4.5.0-preview1-25718-03
del Program.cs
````
Note: Removing Program.cs is necessary to avoid multiple entry-points, which would return an error on build.

4. Open SynTex-Master.csproj file and disable the generation of assembly configuration inside the <PropertyGroup> directive:

````batch
  <PropertyGroup>
	<GenerateAssemblyConfigurationAttribute>false</GenerateAssemblyConfigurationAttribute>
  </PropertyGroup>
````
	
Note: The `PropertyGroup` Directive is inside the <Project Sdk="..."> Directive

5. Build and run the project with the `dotnet` command line tool:
````batch
dotnet run
````
