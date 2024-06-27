using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RabbitSteelModManager.Configuration;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using Reloaded.Universal.Redirector.Structures;
using UndertaleModLib;
using UndertaleModLib.Models;
using static System.Net.Mime.MediaTypeNames;

[module: SkipLocalsInit]
namespace Reloaded.Universal.Redirector;

public class Redirector
{
    private readonly IModLoader _modLoader;
    private ILogger _logger;
    private IModConfig _managerMod;
    private List<ModRedirectorDictionary> _redirections = new List<ModRedirectorDictionary>();
    private ModRedirectorDictionary _customRedirections = new ModRedirectorDictionary();
    private bool _isDisabled = false;
    private UndertaleModLib.UndertaleData _datawin;

    /* Constructor */
    public Redirector(IEnumerable<IModConfigV1> modConfigurations, IModLoader modLoader, ILogger logger, UndertaleData datawin, IModConfig managerMod)
    {
        _modLoader = modLoader;
        _logger = logger;
        _datawin = datawin;
        _managerMod = managerMod;

        foreach (var config in modConfigurations)
        {
            _logger.PrintMessage("CONFIG: " + config, System.Drawing.Color.Green);    
            Add(config);
        }
    }

    /* Business Logic */
    public void AddCustomRedirect(string oldPath, string newPath)
    {
        _customRedirections.FileRedirects[oldPath] = newPath;
    }

    public void RemoveCustomRedirect(string oldPath)
    {
        _customRedirections.FileRedirects.Remove(oldPath);
    }

    public void Add(IModConfigV1 configuration)
    {
        //_logger.PrintMessage("ADDING " + configuration, System.Drawing.Color.Yellow);
        string rsdata = GetDataFolder(configuration.ModId);

        if (Path.Exists(rsdata))
        {
            //_logger.PrintMessage("RSData Folder: " + rsdata, System.Drawing.Color.Yellow);

            #region Sprites
            string spritesPath = Path.Combine(rsdata, "Sprites");
            //_logger.PrintMessage("Checking Sprites in: " + spritesPath, System.Drawing.Color.Red);

            if (Directory.Exists(spritesPath))
            {
                string[] directories = Directory.GetDirectories(spritesPath);
                foreach (string dir in directories)
                {
                    string spritesheetname = Path.GetFileName(dir);
                    //_logger.PrintMessage("Folder: " + spritesheetname, System.Drawing.Color.LightBlue);
                    UndertaleSprite sprite = _datawin.Sprites.ByName(spritesheetname);
                    if (sprite != null)
                    {
                        for (int i = 0; i < sprite.Textures.Count; i++)
                        {
                            var texture = sprite.Textures[i];
                            string imagePath = Path.Combine(dir, $"{spritesheetname}_{i}.png");

                            //_logger.PrintMessage("SPRITE: " + texture.Texture.Name, System.Drawing.Color.AliceBlue);
                            //_logger.PrintMessage($"Loading image from: {imagePath}, color sample: {texture.Texture}", System.Drawing.Color.LightGreen);
                            if (File.Exists(imagePath))
                            {
                                using System.Drawing.Image image = System.Drawing.Image.FromFile(imagePath);
                                {
                                    texture.Texture.ReplaceTexture(image);
                                    //_logger.PrintMessage($"Replaced texture for {texture.Texture.Name} index {i}", System.Drawing.Color.Blue);
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            #region Sounds

            string soundsPath = Path.Combine(rsdata, "Sounds");
            
            _logger.PrintMessage("Checking Sprites in: " + soundsPath, System.Drawing.Color.Red);
            if (Directory.Exists(soundsPath))
            {
                string[] directories = Directory.GetDirectories(soundsPath);
                foreach (string dir in directories)
                {
                    string audiogroup = Path.GetFileName(dir);
                    _logger.PrintMessage("Folder: " + audiogroup, System.Drawing.Color.LightBlue);

                    // Get all .wav and .ogg files in the directory
                    string[] soundFiles = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(file => file.EndsWith(".wav") || file.EndsWith(".ogg")).ToArray();

                    foreach (string soundFilePath in soundFiles)
                    {
                        string soundName = Path.GetFileNameWithoutExtension(soundFilePath);
                        _logger.PrintMessage($"Checking sound file: {soundName}", System.Drawing.Color.AliceBlue);

                        // Try to find the sound by name in the data
                        UndertaleSound sound = _datawin.Sounds.ByName(soundName);
                        if (sound != null)
                        {
                            // This is where your replacement logic will go
                            _logger.PrintMessage($"Found and will replace: {soundName} with {soundFilePath}", System.Drawing.Color.Green);
                            byte[] soundData = File.ReadAllBytes(soundFilePath);
                            sound.AudioFile.Data = soundData;
                        }
                        else
                        {
                            _logger.PrintMessage($"Sound not found in data: {soundName}", System.Drawing.Color.Orange);
                        }
                    }
                    /*if (sound != null)
                    {
                        for (int i = 0; i < sound. .Count; i++)
                        {
                            var texture = sprite.Textures[i];
                            string imagePath = Path.Combine(dir, $"{soundname}_{i}.ogg");

                            //_logger.PrintMessage("SPRITE: " + texture.Texture.Name, System.Drawing.Color.AliceBlue);
                            //_logger.PrintMessage($"Loading image from: {imagePath}, color sample: {texture.Texture}", System.Drawing.Color.LightGreen);
                            if (File.Exists(imagePath))
                            {
                                using System.Drawing.Image sound = System.Drawing.Image.FromFile(imagePath);
                                {
                                    texture.Texture.ReplaceTexture(sound);
                                    //_logger.PrintMessage($"Replaced texture for {texture.Texture.Name} index {i}", System.Drawing.Color.Blue);
                                }
                            }
                        }
                    }
                    */
                }
            }
            #endregion
            // Construct the path to the file
            string tempDataPath = Path.Combine(Path.GetTempPath(), "RabbitManager");
            string exportpath = Path.Combine(tempDataPath, "data.win");
            // Ensure the directory exists
            Directory.CreateDirectory(tempDataPath);

            _logger.PrintMessage("Exporting data temporarily to " + tempDataPath, System.Drawing.Color.LightGreen);
            using (FileStream stream = new FileStream(exportpath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                UndertaleIO.Write(stream, _datawin); // Assuming UndertaleIO.Write correctly handles the stream.
            }
            Add(tempDataPath);
        }

        Add(GetRedirectFolder(configuration.ModId));
    }

    public void Add(string redirectFolder)
    {
        //_logger.PrintMessage("REDIRECT FOLDER: " + redirectFolder, System.Drawing.Color.Yellow);
        _redirections.Add(new ModRedirectorDictionary(redirectFolder));
    }

    internal void Add(string folderPath, string sourceFolder)
    {
        //_logger.PrintMessage("REDIRECT FOLDER: " + folderPath + " | " + sourceFolder, System.Drawing.Color.Yellow);
        _redirections.Add(new ModRedirectorDictionary(folderPath, sourceFolder));
    }

    public void Remove(string redirectFolder, string sourceFolder)
    {
        _redirections = _redirections.Where(x => !x.RedirectFolder.Equals(redirectFolder, StringComparison.OrdinalIgnoreCase) &&
                                                 !x.SourceFolder.Equals(sourceFolder, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public void Remove(string redirectFolder)
    {
        _redirections = _redirections.Where(x => !x.RedirectFolder.Equals(redirectFolder, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public void Remove(IModConfigV1 configuration)
    {
        Remove(GetRedirectFolder(configuration.ModId));
    }

    public bool TryRedirect(string path, out string newPath)
    {
        // Check if disabled.
        newPath = path;
        if (_isDisabled)
            return false;

        // Custom redirections.
        if (_customRedirections.GetRedirection(path, out newPath))
        {
            return true;
        }


        // Doing this in reverse because mods with highest priority get loaded last.
        // We want to look at those mods first.
        for (int i = _redirections.Count - 1; i >= 0; i--)
        {
            if (_redirections[i].GetRedirection(path, out newPath))
            {
                //_logger.PrintMessage("Trying to redirect: " + newPath + " from " + path, System.Drawing.Color.Orange);
                return true;
            }
        }


        return false;
    }

    private string GetRedirectFolder(string modId) => _modLoader.GetDirectoryForModId(modId) + "\\RSMod";
    private string GetDataFolder(string modId) => _modLoader.GetDirectoryForModId(modId) + "\\RSData";

    public void Disable() => _isDisabled = true;
    public void Enable() => _isDisabled = false;
}