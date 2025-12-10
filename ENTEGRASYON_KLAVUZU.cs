// MainWindow.xaml.cs dosyasina EKLENECEK KODLAR:

// 1. USING EKLE (dosya basina):
using Dualshock4Customizer.Helpers;

// 2. CLASS ICINE FIELD'LAR EKLE:
private Model3DGroup _loadedModel;
private GeometryModel3D _lightBarMesh;

// 3. Window_Loaded METODUNU GUNCELLE:
private void Window_Loaded(object sender, RoutedEventArgs e)
{
    LoadGltfModel();
}

// 4. YENÝ METODLARI EKLE:
private void LoadGltfModel()
{
    try
    {
        // glTF modelini yukle (.gltf veya .glb)
        string modelPath = "Models/ds4-controller.gltf"; 
        _loadedModel = GltfModelLoader.LoadGltfModel(modelPath);

        if (_loadedModel != null)
        {
            Debug.WriteLine("? glTF model basariyla yuklendi!");
            
            // Modeli merkeze getir ve olceklendir
            GltfModelLoader.CenterModel(_loadedModel);
            GltfModelLoader.AutoScale(_loadedModel, 4.0);
            
            // Mesh'leri listele (debug)
            Debug.WriteLine("=== Model Mesh Listesi ===");
            GltfModelLoader.ListAllMeshes(_loadedModel);
            
            // Light Bar mesh'ini bul
            // NOT: Light Bar mesh'iniz farkli isimde olabilir!
            _lightBarMesh = GltfModelLoader.FindMeshByName(_loadedModel, "lightbar") 
                         ?? GltfModelLoader.FindMeshByName(_loadedModel, "light") 
                         ?? GltfModelLoader.FindMeshByName(_loadedModel, "bar")
                         ?? GltfModelLoader.FindMeshByName(_loadedModel, "led");
            
            if (_lightBarMesh != null)
            {
                Debug.WriteLine("? Light Bar mesh bulundu!");
                GltfModelLoader.SetupLightBarMaterial(_lightBarMesh, 33, 150, 243);
            }
            else
            {
                Debug.WriteLine("?? Light Bar mesh bulunamadi.");
                Debug.WriteLine("Yukaridaki mesh listesine bakin ve dogru ismi kullanin!");
            }
            
            // Rotation setup
            SetupModelRotation(_loadedModel);
            
            // Eski basit geometriyi kaldir
            ControllerModel.Children.Clear();
            
            // Yeni modeli ekle
            ControllerModel.Children.Add(_loadedModel);
        }
        else
        {
            Debug.WriteLine("?? glTF model yuklenemedi. Fallback kullaniliyor.");
            UseFallbackGeometry();
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"? Model yukleme hatasi: {ex.Message}");
        UseFallbackGeometry();
    }
}

private void UseFallbackGeometry()
{
    _loadedModel = GltfModelLoader.CreateFallbackDS4Model();
    _lightBarMesh = GltfModelLoader.FindMeshByName(_loadedModel, "LightBar");
    SetupModelRotation(_loadedModel);
    ControllerModel.Children.Clear();
    ControllerModel.Children.Add(_loadedModel);
}

private void SetupModelRotation(Model3DGroup model)
{
    _rotationX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -10);
    _rotationY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 20);
    
    var transformGroup = new Transform3DGroup();
    if (model.Transform != null)
        transformGroup.Children.Add(model.Transform);
    transformGroup.Children.Add(new RotateTransform3D(_rotationX));
    transformGroup.Children.Add(new RotateTransform3D(_rotationY));
    model.Transform = transformGroup;
}

// 5. UpdateLightBarColor METODUNU GUNCELLE:
private void UpdateLightBarColor(byte r, byte g, byte b)
{
    Dispatcher.Invoke(() =>
    {
        // XAML basit Light Bar (fallback)
        if (LightBarBrush != null)
        {
            LightBarBrush.Color = Color.FromRgb(r, g, b);
        }
        
        // glTF modelindeki Light Bar
        if (_lightBarMesh != null)
        {
            GltfModelLoader.SetupLightBarMaterial(_lightBarMesh, r, g, b);
        }
    });
}
