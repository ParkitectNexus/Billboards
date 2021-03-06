using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Custom_Scenery.Compression;
using MiniJSON;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Custom_Scenery.CustomScenery
{
    public class BillBoard : Deco
    {
        [Serialized]
        public bool Pn = false;

        [Serialized]
        public string Image;

        public string ImageSource;

        public string Path { get; set; }

        public string BannerPath { get; set; }

        public string Size { get; set; }

        private readonly List<Banner> _banners = new List<Banner>();

        // GUI
        private Vector2 _scrollPosition = Vector2.zero;
        private GUISkin _skin;
        private bool _show;
        private Rect _windowPosition = new Rect(20, 20, 350, 320);

        public override void Start()
        {
            base.Start();

            BannerPath = FilePaths.getFolderPath("Banners");

            // ReSharper disable once PossibleNullReferenceException
            Path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Main)).Location).Replace("bin", ""), "");
            
            if (!Directory.Exists(BannerPath))
            {
                char dsc = System.IO.Path.DirectorySeparatorChar;

                Directory.CreateDirectory(BannerPath);
                Directory.CreateDirectory(BannerPath + dsc + "long");
                Directory.CreateDirectory(BannerPath + dsc + "wide");
                Directory.CreateDirectory(BannerPath + dsc + "square");

                File.Copy(Path + dsc + "Banners" + dsc + "long" + dsc + "long.png", BannerPath + dsc + "long" + dsc + "long.png");
                File.Copy(Path + dsc + "Banners" + dsc + "wide" + dsc + "wide.png", BannerPath + dsc + "wide" + dsc + "wide.png");
                File.Copy(Path + dsc + "Banners" + dsc + "square" + dsc + "square.png", BannerPath + dsc + "square" + dsc + "square.png");
            }
            

            Size = gameObject.name.Split(' ').Last().Split('(').First();

            if (string.IsNullOrEmpty(Image))
            {
                StartCoroutine(LoadRandomBanner());
            }
            else
            {
                Texture2D tex = new Texture2D(1, 1);

                byte[] image = Decompress(Convert.FromBase64String(Image));

                tex.LoadImage(image);

                SetTexture(tex);
            }

            StartCoroutine(LoadGUISkin());

            StartCoroutine(ReloadLocalBanners());
        }

        private IEnumerator LoadGUISkin()
        {
            char dsc = System.IO.Path.DirectorySeparatorChar;

            using (WWW www = new WWW("file://" + Path + dsc + "assetbundle" + dsc + "guiskin"))
            {
                yield return www;

                _skin = www.assetBundle.LoadAsset<GUISkin>("ParkitectGUISkin");

                www.assetBundle.Unload(false);
            }
        }

        public IEnumerator LoadRandomBanner()
        {
            if (Pn)
            {
                WWW www = new WWW("https://parkitectnexus.com/api/screenshots");

                yield return www;

                if (www.error == null)
                {
                    Dictionary<string, object> dict = Json.Deserialize(www.text) as Dictionary<string, object>;

                    Dictionary<string, object> meta = dict["meta"] as Dictionary<string, object>;

                    Dictionary<string, object> pagination = meta["pagination"] as Dictionary<string, object>;

                    long total = (long)pagination["total"];
                    long perPage = (long)pagination["per_page"];

                    int pages = Mathf.CeilToInt(total / perPage);

                    www = new WWW("https://parkitectnexus.com/api/screenshots?page=" + Mathf.RoundToInt(Random.value * pages));

                    yield return www;

                    dict = Json.Deserialize(www.text) as Dictionary<string, object>;

                    List<object> screenshots = dict["data"] as List<object>;

                    Dictionary<string, object> screenshot = screenshots[Mathf.CeilToInt(Random.value * screenshots.Count - 1)] as Dictionary<string, object>;

                    Dictionary<string, object> resource = screenshot["image"] as Dictionary<string, object>;

                    ImageSource = resource["url"] as string;
                }
                else
                {
                    Debug.Log(www.error);
                }
            }
            else
            {
                ImageSource = LoadRandomFromDir(System.IO.Path.Combine(BannerPath, Size));
            }

            StartCoroutine(LoadImage());
        }

        private IEnumerator LoadImage()
        {
            if (Pn)
            {
                WWW www = new WWW(ImageSource);

                yield return www;

                Texture2D tex = new Texture2D(1, 1);
                tex.LoadImage(www.bytes);
                Image = Convert.ToBase64String(Compress(www.bytes));

                SetTexture(tex);
            }
            else
            {
                SetTexture(LoadFromImage(ImageSource));
            }
        }

        private IEnumerator ReloadLocalBanners()
        {
            _banners.Clear();

            string[] files = Directory.GetFiles(System.IO.Path.Combine(BannerPath, Size));

            foreach (string file in files)
            {
                Texture2D tex = LoadFromImage(file);

                yield return null;

                _banners.Add(new Banner { Source = file, Texture = tex });
            }
        }

        private void OnMouseDown()
        {
            if (!GetComponent<BuildableObject>().isPreview && !UIUtility.isMouseOverUIElement())
                _show = true;
        }

        private void OnGUI()
        {
            GUI.skin = _skin;

            if (_show)
            {
                _windowPosition = GUI.Window(Mathf.RoundToInt(transform.position.x * transform.position.z), _windowPosition, BillboardWindow, "Billboard");

                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    _show = false;
                }
            }
        }

        private void BillboardWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 310, 30));
            if (GUI.Button(new Rect(320, 5, 20, 20), "x"))
            {
                _show = false;
            }

            bool orgPn = Pn;

            Pn = GUI.Toggle(new Rect(10, 35, 300, 20), Pn, "Random screenshots from ParkitectNexus.com");
            Pn = !GUI.Toggle(new Rect(10, 55, 300, 20), !Pn, "Local images");


            if (GUI.Button(new Rect(10, 75, 130, 20), "New random banner") || Pn != orgPn)
            {
                StartCoroutine(LoadRandomBanner());
            }

            GUI.Label(new Rect(10, 100, 350, 20), "Banner images are located in:");
            GUI.Label(new Rect(10, 120, 300, 20), BannerPath, new GUIStyle() { fontSize = 10, wordWrap = true });

            if (!Pn)
            {
                _scrollPosition = GUI.BeginScrollView(new Rect(10, 145, 330, 170), _scrollPosition, new Rect(0, 0, 310, (_banners.Count / 5 + 1) * 60));

                for (int x = 0; x < 5; x++)
                {
                    for (int y = 0; y < _banners.Count + 1; y++)
                    {
                        if (_banners.ElementAtOrDefault(x + y * 5) == null)
                            break;

                        if (GUI.Button(new Rect(x * 60, y * 60, 50, 50), _banners[x + y * 5].Texture, new GUIStyle() { padding = new RectOffset(0, 0, 0, 0), border = new RectOffset(0, 0, 0, 0) }))
                        {
                            ImageSource = _banners[x + y * 5].Source;
                            SetTexture(LoadFromImage(ImageSource));
                        }
                    }
                }

                GUI.EndScrollView();
            }
        }

        private void SetTexture(Texture2D tex)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer.gameObject.name.StartsWith("Board"))
                {
                    renderer.material.mainTexture = tex;
                }
            }
        }

        public string LoadRandomFromDir(string dir)
        {
            string[] files = Directory.GetFiles(dir);

            int i = Mathf.Max(0, Mathf.RoundToInt(Random.value * files.Count() - 1));

            string file = files[i];

            return file;
        }

        public Texture2D LoadFromImage(string path)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(path))
            {
                fileData = File.ReadAllBytes(path);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);

                Image = Convert.ToBase64String(Compress(fileData));
            }

            return tex;
        }

        public byte[] Compress(byte[] data)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(data);

            switch (Size)
            {
                case "Long":
                    TextureScale.Bilinear(tex, 1000, 200);
                    break;
                case "Wide":
                    TextureScale.Bilinear(tex, 500, 200);
                    break;
                case "Square":
                    TextureScale.Bilinear(tex, 200, 200);
                    break;

            }

            data = tex.EncodeToJPG();

            using (MemoryStream outStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(outStream, CompressionMode.Compress))
                using (MemoryStream srcStream = new MemoryStream(data))
                {
                    CopyTo(srcStream, gzipStream);
                    //srcStream.CopyTo(gzipStream);
                }
                return outStream.ToArray();
            }
        }

        public byte[] Decompress(byte[] compressed)
        {
            using (MemoryStream inStream = new MemoryStream(compressed))
            using (GZipStream gzipStream = new GZipStream(inStream, CompressionMode.Decompress))
            using (MemoryStream outStream = new MemoryStream())
            {
                CopyTo(gzipStream, outStream);
                //gzipStream.CopyTo(outStream);
                return outStream.ToArray();
            }
        }

        public void CopyTo(Stream input, Stream output)
        {
            byte[] buffer = new byte[4 * 1024];
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) != 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
    }
}
