﻿using Lua14.Data;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Lua14.Lua;

public class LuaRunner
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IDependencyCollection _gameDeps = default!;

    private readonly IDependencyCollection _deps;
    private readonly List<Type> _librariesTypes = [];

    private readonly LuaMod _mod;
    private readonly LuaLogger _logger;
    private readonly NLua.Lua _state = new();

    public LuaRunner(LuaMod mod)
    {
        IoCManager.InjectDependencies(this);
        _mod = mod;
        _logger = new(mod.Config.Name);

        _deps = _gameDeps.FromParent(_gameDeps); // new DependencyCollection(_gameDeps)

        RegisterIoC();
        RegisterLibs();
        LoadLibs();
    }

    private void RegisterIoC()
    {
        _deps.RegisterInstance<NLua.Lua>(_state);
        _deps.RegisterInstance<LuaMod>(_mod);
        _deps.RegisterInstance<LuaLogger>(_logger);
        _deps.RegisterInstance<HarmonyLib.Harmony>(
            new HarmonyLib.Harmony(_mod.Config.Name)
        );
    }

    private void RegisterLibs() {
        var libs = _reflection.GetAllChildren<LuaLibrary>();
        _librariesTypes.AddRange(libs);

        foreach (var lib in libs)
        {
            _deps.Register(lib);
        }
        _deps.BuildGraph();
    }

    private void LoadLibs() {
        foreach (var type in _librariesTypes)
        {
            var library = (LuaLibrary)_deps.ResolveType(type);

            library.Initialize();
            library.Register(_state);
        }
    }

    public object[] ExecuteMain() {
        if (!_mod.TryFindFile(_mod.Config.MainFile, out var file))
            throw new Exception($"No file found with path {_mod.Config.MainFile}");

        return _state.DoString(file.Content, _mod.Config.Name);
    }
}
