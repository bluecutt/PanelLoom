using ComicEditor.Tests;
ComicEditor.Rendering.LegacyGdiBootstrap.Initialize();

var suite = args.Length > 1 && args[0] == "--suite" ? args[1] : "All";
return TestHarness.Run(suite);
