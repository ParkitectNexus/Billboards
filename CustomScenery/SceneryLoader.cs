﻿using System;
using System.Collections.Generic;
using System.IO;
using Custom_Scenery.CustomScenery;
using Custom_Scenery.CustomScenery.Decorators;
using Custom_Scenery.Decorators;
using MiniJSON;
using UnityEngine;

namespace Custom_Scenery
{
    internal class SceneryLoader : MonoBehaviour
    {
        private List<BuildableObject> _sceneryObjects = new List<BuildableObject>();

        public string Path;

        public string Identifier;

        public void LoadScenery()
        {
            char dsc = System.IO.Path.DirectorySeparatorChar;

            try
            {
                var dict = Json.Deserialize(File.ReadAllText(Path + @"/scenery.json")) as Dictionary<string, object>;

                GameObject hider = new GameObject();

                using (WWW www = new WWW("file://" + Path + dsc + "assetbundle" + dsc + "scenery"))
                {
                    if (www.error != null)
                        throw new Exception("Loading had an error:" + www.error);

                    AssetBundle bundle = www.assetBundle;
                    
                    foreach (KeyValuePair<string, object> pair in dict)
                    {
                        try
                        {
                            var options = pair.Value as Dictionary<string, object>;
                            
                            GameObject asset = (new TypeDecorator((string)options["type"])).Decorate(options, bundle);
                            (new PriceDecorator((double)options["price"])).Decorate(asset, options, bundle);
                            (new NameDecorator(pair.Key)).Decorate(asset, options, bundle);

                            if (options.ContainsKey("grid"))
                                (new GridDecorator((bool)options["grid"])).Decorate(asset, options, bundle);

                            if (options.ContainsKey("recolorable"))
                                (new RecolorableDecorator((bool)options["recolorable"])).Decorate(asset, options, bundle);

                            if (options.ContainsKey("category"))
                                (new GroupDecorator()).Decorate(asset, options, bundle);

                            (new ResizeDecorator()).Decorate(asset, options, bundle);

                            DontDestroyOnLoad(asset);

                            BillBoard billBoard = asset.GetComponent<BillBoard>();
                            billBoard.dontSerialize = true;
                            billBoard.isPreview = true;
                            
                            AssetManager.Instance.registerObject(billBoard);
                            _sceneryObjects.Add(asset.GetComponent<BuildableObject>());
                            
                            // hide it from view
                            asset.transform.parent = hider.transform;
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);

                            LogException(e);
                        }
                    }

                    bundle.Unload(false);
                }

                hider.SetActive(false);
            }
            catch(Exception e)
            {
                LogException(e);
            }
        }

        private void LogException(Exception e)
        {
            StreamWriter sw = File.AppendText(Path + @"/mod.log");

            sw.WriteLine(e);

            sw.Flush();

            sw.Close();
        }

        public void UnloadScenery()
        {
            foreach (BuildableObject deco in _sceneryObjects)
            {
                AssetManager.Instance.unregisterObject(deco);
            }
        }
    }
}
