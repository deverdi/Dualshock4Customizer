using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using HelixToolkit.Wpf;
using Assimp;
using Assimp.Configs;
using WpfMaterial = System.Windows.Media.Media3D.Material;
using WpfVector3D = System.Windows.Media.Media3D.Vector3D;

namespace Dualshock4Customizer.Helpers
{
    /// <summary>
    /// Gelismis 3D model yukleme helper - glTF, OBJ, FBX, STL ve 40+ format destegi
    /// </summary>
    public static class UniversalModelLoader
    {
        /// <summary>
        /// glTF, OBJ, FBX, STL ve diger formatlari yukle
        /// </summary>
        public static Model3DGroup LoadModel(string filePath)
        {
            try
            {
                string fullPath = GetFullPath(filePath);
                
                if (!File.Exists(fullPath))
                {
                    System.Diagnostics.Debug.WriteLine($"? Model dosyasi bulunamadi: {fullPath}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"?? Model yukleniyor: {fullPath}");
                System.Diagnostics.Debug.WriteLine($"   Format: {Path.GetExtension(fullPath)}");
                System.Diagnostics.Debug.WriteLine($"   Boyut: {new FileInfo(fullPath).Length / 1024} KB");

                // HelixToolkit'i önce dene (OBJ için daha iyi texture desteði)
                var model = LoadWithHelixToolkit(fullPath);
                
                if (model != null)
                {
                    System.Diagnostics.Debug.WriteLine($"? Model HelixToolkit ile yuklendi! Mesh count: {model.Children.Count}");
                    return model;
                }

                System.Diagnostics.Debug.WriteLine("? HelixToolkit yukleyemedi, Assimp deneniyor...");
                
                // Fallback: Assimp dene
                model = LoadWithAssimp(fullPath);
                
                if (model != null)
                {
                    System.Diagnostics.Debug.WriteLine($"? Model Assimp ile yuklendi!");
                    return model;
                }

                System.Diagnostics.Debug.WriteLine("? Hicbir yukleme yontemi basarili olmadi!");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Model yukleme hatasi: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return null;
            }
        }

        private static string GetFullPath(string filePath)
        {
            if (Path.IsPathRooted(filePath) && File.Exists(filePath))
                return filePath;

            var alternatives = new[]
            {
                filePath,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath),
                Path.Combine(Directory.GetCurrentDirectory(), filePath)
            };

            foreach (var alt in alternatives)
            {
                if (File.Exists(alt))
                    return Path.GetFullPath(alt);
            }

            return Path.GetFullPath(filePath);
        }

        private static Model3DGroup LoadWithHelixToolkit(string filePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("? HelixToolkit ile yukleme baslatildi...");
                
                var importer = new ModelImporter();
                var model = importer.Load(filePath);
                
                if (model != null)
                {
                    System.Diagnostics.Debug.WriteLine($"? HelixToolkit yukleme basarili!");
                    
                    // Texture bilgilerini listele
                    ListModelTextures(model);
                }
                
                return model;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? HelixToolkit yukleme hatasi: {ex.Message}");
                return null;
            }
        }

        private static void ListModelTextures(Model3DGroup model, string indent = "")
        {
            if (model == null) return;

            try
            {
                foreach (var child in model.Children)
                {
                    if (child is GeometryModel3D geo)
                    {
                        if (geo.Material is MaterialGroup matGroup)
                        {
                            foreach (var mat in matGroup.Children)
                            {
                                CheckMaterialTexture(mat, indent);
                            }
                        }
                        else
                        {
                            CheckMaterialTexture(geo.Material, indent);
                        }
                    }
                    else if (child is Model3DGroup subGroup)
                    {
                        ListModelTextures(subGroup, indent + "  ");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{indent}? Texture liste hatasi: {ex.Message}");
            }
        }

        private static void CheckMaterialTexture(WpfMaterial material, string indent)
        {
            if (material == null) return;

            if (material is DiffuseMaterial diffuse)
            {
                if (diffuse.Brush is ImageBrush imgBrush)
                {
                    var source = imgBrush.ImageSource;
                    if (source is BitmapImage bitmap)
                    {
                        System.Diagnostics.Debug.WriteLine($"{indent}?? Texture: {bitmap.UriSource}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"{indent}?? Texture: (ImageSource)");
                    }
                }
                else if (diffuse.Brush is SolidColorBrush solidBrush)
                {
                    System.Diagnostics.Debug.WriteLine($"{indent}?? Solid Color: {solidBrush.Color}");
                }
            }
            else if (material is EmissiveMaterial emissive)
            {
                System.Diagnostics.Debug.WriteLine($"{indent}?? Emissive Material");
            }
        }

        private static Model3DGroup LoadWithAssimp(string filePath)
        {
            try
            {
                // Dosya kontrolü
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"? DOSYA YOK: {filePath}");
                    return null;
                }
                
                var fileInfo = new FileInfo(filePath);
                System.Diagnostics.Debug.WriteLine($"? Dosya bulundu: {fileInfo.Length} bytes");
                
                var importer = new AssimpContext();
                
                // Post-processing ayarlari
                var flags = PostProcessSteps.Triangulate |
                           PostProcessSteps.GenerateSmoothNormals |
                           PostProcessSteps.FlipUVs |
                           PostProcessSteps.JoinIdenticalVertices;

                System.Diagnostics.Debug.WriteLine("? Assimp ile yukleme baslatildi...");
                
                Scene scene = null;
                try
                {
                    scene = importer.ImportFile(filePath, flags);
                }
                catch (Exception importEx)
                {
                    System.Diagnostics.Debug.WriteLine($"? Assimp ImportFile hatasi: {importEx.Message}");
                    return null;
                }
                
                if (scene == null || !scene.HasMeshes)
                {
                    System.Diagnostics.Debug.WriteLine("? Assimp: Scene yuklenemedi veya mesh yok");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"? Assimp scene yuklendi: {scene.MeshCount} mesh, {scene.MaterialCount} material");

                var group = new Model3DGroup();
                var baseDir = Path.GetDirectoryName(filePath);

                // Her mesh'i WPF geometrisine donustur
                for (int i = 0; i < scene.MeshCount; i++)
                {
                    var mesh = scene.Meshes[i];
                    System.Diagnostics.Debug.WriteLine($"   Mesh {i}: {mesh.Name} ({mesh.VertexCount} vertices, {mesh.FaceCount} faces)");
                    
                    var wpfMesh = ConvertAssimpMeshToWpf(mesh, scene, baseDir);
                    if (wpfMesh != null)
                    {
                        group.Children.Add(wpfMesh);
                    }
                }

                return group;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Assimp yukleme hatasi: {ex.Message}");
                return null;
            }
        }

        private static GeometryModel3D ConvertAssimpMeshToWpf(Assimp.Mesh assimpMesh, Scene scene, string baseDir)
        {
            try
            {
                var mesh = new MeshGeometry3D();

                // Vertices
                foreach (var vertex in assimpMesh.Vertices)
                {
                    mesh.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
                }

                // Normals
                if (assimpMesh.HasNormals)
                {
                    foreach (var normal in assimpMesh.Normals)
                    {
                        mesh.Normals.Add(new WpfVector3D(normal.X, normal.Y, normal.Z));
                    }
                }

                // Texture coordinates
                if (assimpMesh.HasTextureCoords(0))
                {
                    foreach (var uv in assimpMesh.TextureCoordinateChannels[0])
                    {
                        mesh.TextureCoordinates.Add(new System.Windows.Point(uv.X, 1.0 - uv.Y)); // Flip V
                    }
                    System.Diagnostics.Debug.WriteLine($"      UV Coordinates: {mesh.TextureCoordinates.Count}");
                }

                // Indices (triangles)
                foreach (var face in assimpMesh.Faces)
                {
                    if (face.IndexCount == 3)
                    {
                        mesh.TriangleIndices.Add(face.Indices[0]);
                        mesh.TriangleIndices.Add(face.Indices[1]);
                        mesh.TriangleIndices.Add(face.Indices[2]);
                    }
                }

                // Material
                WpfMaterial material = new DiffuseMaterial(new SolidColorBrush(Colors.LightGray));
                
                if (assimpMesh.MaterialIndex >= 0 && assimpMesh.MaterialIndex < scene.MaterialCount)
                {
                    var assimpMaterial = scene.Materials[assimpMesh.MaterialIndex];
                    material = ConvertAssimpMaterial(assimpMaterial, baseDir);
                }

                var model = new GeometryModel3D(mesh, material);
                
                // Mesh adini sakla
                if (!string.IsNullOrEmpty(assimpMesh.Name))
                {
                    DependencyPropertyHelper.SetName(model, assimpMesh.Name);
                    System.Diagnostics.Debug.WriteLine($"      Mesh adi: '{assimpMesh.Name}'");
                }

                return model;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Mesh donusturme hatasi: {ex.Message}");
                return null;
            }
        }

        private static WpfMaterial ConvertAssimpMaterial(Assimp.Material assimpMaterial, string baseDir)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"      === TEXTURE DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"      Material: {assimpMaterial.Name}");
                System.Diagnostics.Debug.WriteLine($"      Base Dir: {baseDir}");
                
                // Diffuse texture
                if (assimpMaterial.HasTextureDiffuse)
                {
                    var texturePath = assimpMaterial.TextureDiffuse.FilePath;
                    System.Diagnostics.Debug.WriteLine($"      Texture (MTL): {texturePath}");
                    
                    // Try multiple possible texture locations
                    var possiblePaths = new[]
                    {
                        Path.Combine(baseDir, texturePath),
                        Path.Combine(baseDir, "textures", texturePath),
                        Path.Combine(baseDir, Path.GetFileName(texturePath))
                    };
                    
                    foreach (var testPath in possiblePaths)
                    {
                        System.Diagnostics.Debug.WriteLine($"      Trying: {testPath}");
                        if (File.Exists(testPath))
                        {
                            System.Diagnostics.Debug.WriteLine($"      ? TEXTURE FOUND!");
                            try
                            {
                                var bitmap = new BitmapImage(new Uri(testPath, UriKind.Absolute));
                                var brush = new ImageBrush(bitmap)
                                {
                                    ViewportUnits = BrushMappingMode.Absolute,
                                    TileMode = TileMode.Tile
                                };
                                System.Diagnostics.Debug.WriteLine($"      ? Texture loaded successfully!");
                                return new DiffuseMaterial(brush);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"      ? Texture load error: {ex.Message}");
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"      ? TEXTURE NOT FOUND!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"      No diffuse texture");
                }

                // Emissive (Light Bar icin!)
                if (assimpMaterial.HasColorEmissive)
                {
                    var emissive = assimpMaterial.ColorEmissive;
                    if (emissive.R > 0 || emissive.G > 0 || emissive.B > 0)
                    {
                        var color = Color.FromRgb(
                            (byte)(emissive.R * 255),
                            (byte)(emissive.G * 255),
                            (byte)(emissive.B * 255)
                        );
                        System.Diagnostics.Debug.WriteLine($"      ? Emissive: RGB({color.R}, {color.G}, {color.B})");
                        return new EmissiveMaterial(new SolidColorBrush(color));
                    }
                }

                // Diffuse color
                if (assimpMaterial.HasColorDiffuse)
                {
                    var diffuse = assimpMaterial.ColorDiffuse;
                    var color = Color.FromRgb(
                        (byte)(diffuse.R * 255),
                        (byte)(diffuse.G * 255),
                        (byte)(diffuse.B * 255)
                    );
                    System.Diagnostics.Debug.WriteLine($"      ? Diffuse Color: RGB({color.R}, {color.G}, {color.B})");
                    return new DiffuseMaterial(new SolidColorBrush(color));
                }

                System.Diagnostics.Debug.WriteLine($"      ? Using default gray");
                return new DiffuseMaterial(new SolidColorBrush(Colors.LightGray));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"      ? Material error: {ex.Message}");
                return new DiffuseMaterial(new SolidColorBrush(Colors.LightGray));
            }
        }

        public static void CenterModel(Model3DGroup model)
        {
            if (model == null) return;

            try
            {
                var bounds = model.Bounds;
                var center = new Point3D(
                    bounds.X + bounds.SizeX / 2,
                    bounds.Y + bounds.SizeY / 2,
                    bounds.Z + bounds.SizeZ / 2
                );

                var transformGroup = model.Transform as Transform3DGroup ?? new Transform3DGroup();
                transformGroup.Children.Add(new TranslateTransform3D(-center.X, -center.Y, -center.Z));
                model.Transform = transformGroup;
                System.Diagnostics.Debug.WriteLine($"? Model merkezlendi: {center}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Merkez hatasi: {ex.Message}");
            }
        }

        public static void AutoScale(Model3DGroup model, double targetSize = 5.0)
        {
            if (model == null) return;

            try
            {
                var bounds = model.Bounds;
                var maxDim = Math.Max(Math.Max(bounds.SizeX, bounds.SizeY), bounds.SizeZ);
                
                if (maxDim > 0)
                {
                    double scale = targetSize / maxDim;
                    var transformGroup = model.Transform as Transform3DGroup ?? new Transform3DGroup();
                    transformGroup.Children.Add(new ScaleTransform3D(scale, scale, scale));
                    model.Transform = transformGroup;
                    System.Diagnostics.Debug.WriteLine($"? Otomatik olceklendirme: {scale:F3}x");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Scale hatasi: {ex.Message}");
            }
        }

        public static GeometryModel3D FindMeshByName(Model3DGroup group, string nameContains)
        {
            if (group == null || string.IsNullOrEmpty(nameContains)) return null;

            try
            {
                foreach (var child in group.Children)
                {
                    if (child is GeometryModel3D geo)
                    {
                        var name = DependencyPropertyHelper.GetName(geo);
                        if (name != null && name.ToLower().Contains(nameContains.ToLower()))
                        {
                            System.Diagnostics.Debug.WriteLine($"? Mesh bulundu: '{name}'");
                            return geo;
                        }
                    }
                    else if (child is Model3DGroup subGroup)
                    {
                        var found = FindMeshByName(subGroup, nameContains);
                        if (found != null) return found;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Mesh arama hatasi: {ex.Message}");
            }

            return null;
        }

        public static void SetupLightBarMaterial(GeometryModel3D lightBarMesh, byte r, byte g, byte b)
        {
            if (lightBarMesh == null) return;

            try
            {
                var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
                var material = new EmissiveMaterial(brush);
                lightBarMesh.Material = material;
                System.Diagnostics.Debug.WriteLine($"? Light Bar rengi: RGB({r}, {g}, {b})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Light Bar hatasi: {ex.Message}");
            }
        }

        public static void ListAllMeshes(Model3DGroup group, string indent = "")
        {
            if (group == null) return;

            try
            {
                int count = 0;
                foreach (var child in group.Children)
                {
                    if (child is GeometryModel3D geo)
                    {
                        count++;
                        var name = DependencyPropertyHelper.GetName(geo) ?? $"Mesh_{count}";
                        System.Diagnostics.Debug.WriteLine($"{indent}?? {name}");
                        
                        if (geo.Material is EmissiveMaterial)
                        {
                            System.Diagnostics.Debug.WriteLine($"{indent}   ?? Emissive (Light Bar olabilir!)");
                        }
                        
                        if (geo.Material is MaterialGroup matGroup)
                        {
                            foreach (var mat in matGroup.Children)
                            {
                                if (mat is DiffuseMaterial diffuse && diffuse.Brush is ImageBrush)
                                {
                                    System.Diagnostics.Debug.WriteLine($"{indent}   ?? Has texture");
                                }
                            }
                        }
                        else if (geo.Material is DiffuseMaterial diffuse)
                        {
                            if (diffuse.Brush is ImageBrush)
                            {
                                System.Diagnostics.Debug.WriteLine($"{indent}   ?? Has texture");
                            }
                        }
                    }
                    else if (child is Model3DGroup subGroup)
                    {
                        System.Diagnostics.Debug.WriteLine($"{indent}?? Group:");
                        ListAllMeshes(subGroup, indent + "  ");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Liste hatasi: {ex.Message}");
            }
        }

        public static Model3DGroup CreateFallbackDS4Model()
        {
            System.Diagnostics.Debug.WriteLine("? Fallback DS4 geometri olusturuluyor...");
            
            var group = new Model3DGroup();

            try
            {
                var bodyMesh = new MeshBuilder();
                bodyMesh.AddBox(new Point3D(0, 0, 0), 4, 2, 1);
                var bodyGeo = new GeometryModel3D
                {
                    Geometry = bodyMesh.ToMesh(),
                    Material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(44, 44, 44)))
                };
                group.Children.Add(bodyGeo);

                var touchpadMesh = new MeshBuilder();
                touchpadMesh.AddBox(new Point3D(0, 0.5, 0.51), 2, 0.5, 0.01);
                var touchpadGeo = new GeometryModel3D
                {
                    Geometry = touchpadMesh.ToMesh(),
                    Material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(26, 26, 26)))
                };
                group.Children.Add(touchpadGeo);

                var lightBarMesh = new MeshBuilder();
                lightBarMesh.AddBox(new Point3D(0, 1, 0.52), 3, 0.15, 0.02);
                var lightBarGeo = new GeometryModel3D
                {
                    Geometry = lightBarMesh.ToMesh(),
                    Material = new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(33, 150, 243)))
                };
                DependencyPropertyHelper.SetName(lightBarGeo, "LightBar");
                group.Children.Add(lightBarGeo);

                System.Diagnostics.Debug.WriteLine("? Fallback hazir");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Fallback hatasi: {ex.Message}");
            }

            return group;
        }
    }

    internal static class DependencyPropertyHelper
    {
        private static readonly DependencyProperty NameProperty = 
            DependencyProperty.RegisterAttached("Name", typeof(string), typeof(DependencyPropertyHelper));

        public static string GetName(DependencyObject obj)
        {
            return obj.GetValue(NameProperty) as string;
        }

        public static void SetName(DependencyObject obj, string value)
        {
            obj.SetValue(NameProperty, value);
        }
    }
}
