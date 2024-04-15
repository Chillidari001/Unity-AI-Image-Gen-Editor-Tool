using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
//using OpenAI_API;
//using OpenAI_API.Chat;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
//using OpenAI_API.Images;
using System.Threading.Tasks;
using OpenAI_API.Models;
using UnityEditor.VersionControl;
using System.Collections;
using UnityEngine.Networking;
using OpenAI;
using OpenAI.Images;
using OpenAI.Models;
using System.IO;
using NUnit.Framework;
using System.Security.Cryptography;
using System.Net.Sockets;
using UnityEditor.Search;
using System.Text;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;

public class ImageGenerationWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    VisualElement root;
    EnumField API;
    private enum api_choice
    {
        Open_AI,
        Stable_Diffusion
    }
    api_choice chosen_api = api_choice.Open_AI;
    TextField api_key_input;
    string api_key;
    Button save_api_key_button;
    TextField image_generation_prompt;
    Button generate_button;
    Texture2D selected_texture;
    Texture2D image_texture;
    Image generated_image;
    int image_x;
    int image_y;
    string user_input;
    private enum tool_function
    {
        Text_To_Image,
        Image_To_Image_edit,
        Image_To_Image_Variation
    }
    tool_function chosen_function = tool_function.Text_To_Image;
    EnumField image_resolution;
    /*        ImageSize.1024
            ImageSize._256
            ImageSize._512*/
    private enum image_quality
    {
        standard,
        hd
    }
    image_quality chosen_quality = image_quality.standard;

    private enum style
    {
        natural,
        vivid
    }
    style chosen_style = style.natural;
    EnumField style_field;
    public enum Direction
    {
        North,
        East,
        South,
        West
    }
    DropdownField num_of_images;
    int num_images = 1;
    DropdownField image_size;
    string chosen_image_size = "1024x1024";
    Button save_button;
    EnumField function_choice;
    UnityEditor.UIElements.ObjectField base_image_select;
    Image base_image;
    EnumField image_to_image_size;
    public enum Image_Size
    {
        Small,
        Medium,
        Large
    }
    ImageSize size = ImageSize.Small;

    UnityEditor.UIElements.ObjectField mask_image_select;
    Texture2D mask_texture;

    Toolbar toolbar;
    bool help_selected = false;

    private OpenAIClient api;
    //private OpenAIAPI api;
    // private List<ChatMessage> messages;

    //FOR STABLE DIFFUSION IMPLEMENTATION USING INVOKEAI BACKEND
    //SCRAPPED SCRAPPED SCRAPPED SCRAPPED SCRAPPED
    //---------------------------------------------------------
    /*
    static UnityWebRequest www;
    static string url = "http://127.0.0.1:9090/";
    static string sd_root_folder = "";*/

    /*{"prompt":"empty","iterations":"1","steps":"55","cfg_scale":"7.5","sampler_name":"k_lms","width":"512","height":"512","seed":"-1","variation_amount":"0",
     "with_variations":"","initimg":null,"strength":"0.75","fit":"on","gfpgan_strength":"0.8","upscale_level":"","upscale_strength":"0.75","initmg_name":""}*/
    /*
    //string used in original string user_input from above
    private int iterations = 1;
    private int steps = 20;
    private float cfg_scale = 7.5f;
    private string sampler_name = "k_lms"; //TODO dropdown list?
    private int width = 512;
    private int height = 512;
    private int seed = -1; //TODO -1 as random?
    private float variation_amount = 0;
    private string with_variations = "";
    private System.Object initimg = null; //TODO upload image?
    private float strength = 0.75f;
    private bool fit = true; //TODO as "on/off" toggle
    private float gfpgan_strength = 0.8f;
    private string upscale_level = "";
    private float upscale_strength = 0.75f;
    private string initimg_name = "";
    */

    string url = "https://api.openai.com/v1/images/generations";
    string image_edit_url = "https://api.openai.com/v1/images/edits";
    string image_variation_url = "https://api.openai.com/v1/images/variations";

    private void OnEnable()
    {
        image_x = 1280;
        image_y = 720;
        //api key not in source code. keep it secret keep it safe! use below for testing!
        //api = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User));
        //api = new OpenAIAPI(Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User));
        Debug.Log(Application.persistentDataPath);
    }

    [MenuItem("AI Tools/Image Generator")]
    public static void ShowWindow()
    {
        ImageGenerationWindow wnd = GetWindow<ImageGenerationWindow>();
        wnd.titleContent = new GUIContent("Image Generator");
        ImageGenerationWindow tabs = (ImageGenerationWindow)GetWindow(typeof(ImageGenerationWindow));
        tabs.minSize = new Vector2(400, 400);
        tabs.maxSize = new Vector2(1920, 1080);
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        root = rootVisualElement;

        toolbar = new Toolbar();
        var button = new ToolbarButton(() => { help_selected = false; }) { text = "Image Generator" };
        toolbar.Add(button);
        var button2 = new ToolbarButton(() => { help_selected = true; }) { text = "Help" };
        toolbar.Add(button2);
        root.Add(toolbar);
        // VisualElements objects can contain other VisualElement following a tree hierarchy.

        // Instantiate UXML
        //VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        //root.Add(labelFromUXML);

        API = new EnumField("Image Generation API:", api_choice.Open_AI);
        root.Add(API);
        API.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            API.value = evt.newValue;
            chosen_api = (api_choice)API.value;
            Debug.Log(chosen_api);
        });

        api_key_input = new TextField("API Key:");
        root.Add(api_key_input);

        save_api_key_button = new Button();
        save_api_key_button.text = "Use OPENAI API Key" +
            "                                                                                                        " +
            "THIS KEY IS NOT STORED AND MUST NOT BE SHARED WITH OTHERS";
        save_api_key_button.style.whiteSpace = WhiteSpace.Normal;
        save_api_key_button.style.height = 40;
        root.Add(save_api_key_button);
        save_api_key_button.clicked += UseAPIKey;

        function_choice = new EnumField("Function Choice:", tool_function.Text_To_Image);
        root.Add(function_choice);
        function_choice.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            function_choice.value = evt.newValue;
            chosen_function = (tool_function)function_choice.value;
            Debug.Log(chosen_function);
        });

        base_image_select = new UnityEditor.UIElements.ObjectField("Base Image Select:");
        base_image_select.objectType = typeof(Texture2D);
        root.Add(base_image_select);
        base_image_select.RegisterValueChangedCallback<UnityEngine.Object>(ImageSelected);

        base_image = new Image();
        //generated_image.style.backgroundImage = new Texture2D(image_x, image_y);
        base_image.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/placeholder.png");
        base_image.style.height = 400f;
        base_image.style.width = 500f;
        //generated_image.scaleMode = ScaleMode.ScaleToFit;
        root.Add(base_image);

        mask_image_select = new UnityEditor.UIElements.ObjectField("Mask Image Select:");
        mask_image_select.objectType = typeof(Texture2D);
        root.Add(mask_image_select);
        mask_image_select.RegisterValueChangedCallback<UnityEngine.Object>(MaskSelected);

        image_generation_prompt = new TextField("Prompt:");
        image_generation_prompt.multiline = true;
        image_generation_prompt.style.height = 50f;
        root.Add(image_generation_prompt);

        image_resolution = new EnumField("Image Resolution:", image_quality.standard);
        root.Add(image_resolution);
        image_resolution.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            image_resolution.value = evt.newValue;
            chosen_quality = (image_quality)image_resolution.value;
            Debug.Log(chosen_quality);
        });

        style_field = new EnumField("Style:", style.natural);
        root.Add(style_field);
        style_field.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            style_field.value = evt.newValue;
            chosen_style = (style)image_resolution.value;
            Debug.Log(chosen_style);
        });

        num_of_images = new DropdownField("Number of images:", new List<string> { "1", "2", "3", "4" }, 0);
        num_of_images.index = 0;
        root.Add(num_of_images);
        num_of_images.RegisterValueChangedCallback((evt) =>
        {
            num_images = Convert.ToInt32(num_of_images.value);
            Debug.Log(num_images);
        });

        image_size = new DropdownField("Image Size:", new List<string> { "256x256", "512x512", "1024x1024", "1024x1792", "1792x1024" }, 0);
        image_size.index = 0;
        root.Add(image_size);
        image_size.RegisterValueChangedCallback((evt) =>
        {
            chosen_image_size = image_size.value;
            Debug.Log(chosen_image_size);
        });
        
        image_to_image_size = new EnumField("Image Size:", ImageSize.Small);
        root.Add(image_to_image_size);
        image_to_image_size.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            image_to_image_size.value = evt.newValue;
            size = (ImageSize)image_to_image_size.value;
            Debug.Log(size);
        });
        
        generate_button = new Button();
        generate_button.text = "Generate Image";
        generate_button.style.height = 40;
        root.Add(generate_button);
        generate_button.clicked += GenerateImage;

        generated_image = new Image();
        //generated_image.style.backgroundImage = new Texture2D(image_x, image_y);
        generated_image.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/placeholder.png");
        generated_image.style.height = 400f;
        generated_image.style.width = 500f;
        //generated_image.scaleMode = ScaleMode.ScaleToFit;
        root.Add(generated_image);

        save_button = new Button();
        save_button.text = "Save Image";
        save_button.style.height = 40;
        root.Add(save_button);
        save_button.clicked += SaveImage;

    }

    public void OnGUI()
    {

        //num_of_tabs = GUILayout.Toolbar(num_of_tabs, tab_names);
        /*float padding = 100;
        Rect area = new Rect(padding, padding,
             position.width - padding * 2f, position.height - padding * 2f);

        GUILayout.BeginArea(area);
       
        GUILayout.EndArea();*/

        if(help_selected)
        {
            GUILayout.Label("TEST");
            GUILayout.FlexibleSpace();
        }
    }

    private async void GenerateImage()
    {
        user_input = image_generation_prompt.value;

        if (chosen_api == api_choice.Open_AI)
        {
            //image_x = image_x_input.value;
            //image_y = image_y_input.value;
            Debug.Log(user_input);
            Debug.Log("Image definitions will be: " + image_x + "," + image_y);

            /*async Task<ImageResult> CreateImageAsync(ImageGenerationRequest request)
            {
                var result = await api.ImageGenerations.CreateImageAsync(new ImageGenerationRequest(image_generation_prompt.value, 1, ImageSize._1024));

                Debug.Log(result);
                return result;
            }*/

            /*var result = await api.ImageGenerations.CreateImageAsync(new ImageGenerationRequest()
            {
                NumOfImages = num_images,
                Prompt = user_input,
                ResponseFormat = ImageResponseFormat.Url,
                Size = ImageSize._256,
            });

            Debug.Log(result.Data[0].Url);
            DownloadImage(result.Data[0].Url);*/

            /*var result = await api.ImageGenerations.CreateImageAsync(new ImageGenerationRequest("fat cat", 1, ImageSize._512));
            Debug.Log(result.Data[0].Url);*/
            if(chosen_function == tool_function.Text_To_Image)
            {
                user_input = user_input.Insert(0, " \"");
                user_input += "\"";
                string num_images_str = num_images.ToString();
                //num_images_str.Insert(0, "\"");
                //num_images_str += "\"";
                chosen_image_size = chosen_image_size.Insert(0, "\"");
                chosen_image_size += "\"";
                string chosen_quality_str = chosen_quality.ToString();
                chosen_quality_str = chosen_quality_str.Insert(0, "\"");
                chosen_quality_str += "\"";
                string chosen_style_str = chosen_style.ToString();
                chosen_style_str = chosen_style_str.Insert(0, "\"");
                chosen_style_str += "\"";

                string body = "{\"prompt\":" + user_input + ",\"n\":" + num_images_str + ",\"size\":" + chosen_image_size + ",\"quality\":" + chosen_quality_str + ", \"style\":" + chosen_style_str + ",\"model\": \"dall-e-3\",\"response_format\":\"url\"}";
                Debug.Log(body);
                //Debug.Log(body2);

                // Prepare data for the POST request
                var data = Encoding.ASCII.GetBytes(body);
                Debug.Log(data);

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                // Authentication
                if (api_key != null)
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    request.PreAuthenticate = true;
                    request.Headers.Add("Authorization", "Bearer " + api_key);
                }
                else
                {
                    request.Credentials = CredentialCache.DefaultCredentials;
                }

                // Perform request
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                // Retrieve response
                var response = (HttpWebResponse)request.GetResponse();
                Debug.Log("Request response: " + response.StatusCode);

                var response_string = new StreamReader(response.GetResponseStream()).ReadToEnd();
                Debug.Log(response_string);

                //deserialize json
                dynamic json_response = JsonConvert.DeserializeObject<dynamic>(response_string);
                Debug.Log(json_response.created);
                Debug.Log(json_response.data);
                Debug.Log(json_response.data[0].url);

                string response_url = Convert.ToString(json_response.data[0].url);
                Debug.Log(response_url);
                EditorCoroutineUtility.StartCoroutineOwnerless(DownloadImage(response_url));

            }
                
            if(chosen_function == tool_function.Image_To_Image_edit)
            {

                user_input = user_input.Insert(0, " \"");
                user_input += "\"";
                string num_images_str = num_images.ToString();
                //num_images_str.Insert(0, "\"");
                //num_images_str += "\"";
                //chosen_image_size = chosen_image_size.Insert(0, "\"");
                //chosen_image_size += "\"";
                string chosen_style_str = chosen_style.ToString();
                chosen_style_str = chosen_style_str.Insert(0, "\"");
                chosen_style_str += "\"";
                
                string texture_path = Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(selected_texture));
                texture_path = texture_path.Replace("/", "\\");
                texture_path = texture_path.Insert(0, "\"");
                texture_path += "\"";
                string mask_path = Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(mask_texture));
                mask_path = mask_path.Replace("/", "\\");
                mask_path = mask_path.Insert(0, "\"");
                mask_path += "\"";

                var httpClient = new HttpClient();
                var multipartContent = new MultipartFormDataContent();

                // Add image file
                var imageContent = new ByteArrayContent(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(selected_texture))));
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                multipartContent.Add(imageContent, "image", "image.png");

                // Add mask file, if applicable
                var maskContent = new ByteArrayContent(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(mask_texture))));
                maskContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                multipartContent.Add(maskContent, "mask", "mask.png");

                // Add other parameters
                multipartContent.Add(new StringContent(user_input), "prompt");
                multipartContent.Add(new StringContent(num_images.ToString()), "n");
                multipartContent.Add(new StringContent(chosen_image_size), "size");
                multipartContent.Add(new StringContent("url"), "response_format"); // Assuming you're sending these exact keys

                // Setup request
                var request = new HttpRequestMessage(HttpMethod.Post, image_edit_url);
                request.Headers.Add("Authorization", "Bearer " + api_key);
                request.Content = multipartContent;

                // Send request
                var response = await httpClient.SendAsync(request);

                // Check response
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    Debug.Log(responseString);
                    // Process response
                    // Assuming responseString contains the JSON response
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                    // Extract the URL
                    string responseUrl = jsonResponse.data[0].url.ToString();
                    Debug.Log("Image URL: " + responseUrl);
                    EditorCoroutineUtility.StartCoroutineOwnerless(DownloadImage(responseUrl));
                }
                else
                {
                    // Handle error
                    var errorResponseString = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Error: {response.StatusCode}. Details: {errorResponseString}");
                }
            }

            if (chosen_function == tool_function.Image_To_Image_Variation)
            {
                string chosen_style_str = chosen_style.ToString();
                chosen_style_str = chosen_style_str.Insert(0, "\"");
                chosen_style_str += "\"";

                string texture_path = Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(selected_texture));
                texture_path = texture_path.Replace("/", "\\");
                texture_path = texture_path.Insert(0, "\"");
                texture_path += "\"";


                var httpClient = new HttpClient();
                var multipartContent = new MultipartFormDataContent();

                // Add image file
                var imageContent = new ByteArrayContent(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(selected_texture))));
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                multipartContent.Add(imageContent, "image", "image.png");

                // Add other parameters
                multipartContent.Add(new StringContent(num_images.ToString()), "n");
                multipartContent.Add(new StringContent(chosen_image_size), "size");
                multipartContent.Add(new StringContent("url"), "response_format"); // Assuming you're sending these exact keys

                // Setup request
                var request = new HttpRequestMessage(HttpMethod.Post, image_variation_url);
                request.Headers.Add("Authorization", "Bearer " + api_key);
                request.Content = multipartContent;

                // Send request
                var response = await httpClient.SendAsync(request);

                // Check response
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    Debug.Log(responseString);
                    // Process response
                    // Assuming responseString contains the JSON response
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                    // Extract the URL
                    string responseUrl = jsonResponse.data[0].url.ToString();
                    Debug.Log("Image URL: " + responseUrl);
                    EditorCoroutineUtility.StartCoroutineOwnerless(DownloadImage(responseUrl));
                }
                else
                {
                    // Handle error
                    var errorResponseString = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Error: {response.StatusCode}. Details: {errorResponseString}");
                }
               
            }

        }
    }

    //not using anymore!
    /*static void EditorWebRequestUpdate()
    {
        //https://blog.cyberiansoftware.com.ar/post/149707644965/web-requests-from-unity-editor
        if (!www.isDone) return;

        if (www.isNetworkError)
        {
            Debug.Log("Error: " + www.error);
        }
        else
        {
            Debug.Log("Recieved: " + www.downloadHandler.text);
        }

        EditorApplication.update -= EditorWebRequestUpdate;
    }*/

    public void SaveImage()
    {
        Debug.Log("Image Save Button Clicked!");
        var path = EditorUtility.SaveFilePanel(
            "Save Generated Image In Project Directory",
            Application.dataPath,
            "AIGeneratedImage",
            "png");
        byte[] bytes = image_texture.EncodeToPNG();
        if(string.IsNullOrEmpty(path))
        {
            return;
        }
        File.WriteAllBytes(path, bytes);

        string path_string = path;
        int asset_index = path_string.IndexOf("Assets", StringComparison.Ordinal);
        string file_path = path_string.Substring(asset_index, path.Length - asset_index);
        AssetDatabase.ImportAsset(file_path);
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = (Texture2D)AssetDatabase.LoadAssetAtPath(file_path, typeof(Texture2D));
        Debug.Log("Filepath of saved generated image is: " + file_path);
    }

    //from pale bone on SO.
    //https://stackoverflow.com/questions/31765518/how-to-load-an-image-from-url-with-unity
    IEnumerator DownloadImage(string MediaUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError)
            Debug.Log(request.error);
        else
            image_texture = new Texture2D(2, 2);
            image_texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            generated_image.style.backgroundImage = image_texture;
            Debug.Log("Background image set to download handler texture.");
    }

    IEnumerator DownloadImage2(string media_url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(media_url);
        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError)
        {
            Debug.Log(request.error);
        }
        else
        {
            image_texture = new Texture2D(2, 2);
            image_texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            generated_image.style.backgroundImage = image_texture;
            Debug.Log("Background image set to download handler texture.");
        }
    }

    private void ImageSelected(ChangeEvent<UnityEngine.Object> evt)
    {
        if(evt.newValue == null)
        {
            selected_texture = null;    
            return;
        }
        var output_name = evt.newValue.name + "Modified";
        selected_texture = evt.newValue as Texture2D;
        base_image.style.backgroundImage = selected_texture;
    }

    private void MaskSelected(ChangeEvent<UnityEngine.Object> evt)
    {
        if (evt.newValue == null)
        {
            mask_texture = null;
            return;
        }
        mask_texture = evt.newValue as Texture2D;
    }

    public void Update()
    {
        if(help_selected == false)
        {
            if (chosen_api == api_choice.Open_AI)
            {
                if (chosen_function == tool_function.Text_To_Image)
                {
                    API.style.display = DisplayStyle.Flex;
                    api_key_input.style.display = DisplayStyle.Flex;
                    save_api_key_button.style.display = DisplayStyle.Flex;
                    function_choice.style.display = DisplayStyle.Flex;
                    image_generation_prompt.style.display = DisplayStyle.Flex;
                    base_image_select.style.display = DisplayStyle.None;
                    base_image.style.display = DisplayStyle.None;
                    mask_image_select.style.display = DisplayStyle.None;
                    image_resolution.style.display = DisplayStyle.Flex;
                    style_field.style.display = DisplayStyle.Flex;
                    num_of_images.style.display = DisplayStyle.None;
                    image_size.style.display = DisplayStyle.Flex;
                    image_to_image_size.style.display = DisplayStyle.None;
                    generate_button.style.display = DisplayStyle.Flex;
                    generated_image.style.display = DisplayStyle.Flex;
                    save_button.style.display = DisplayStyle.Flex;
                }
                if (chosen_function == tool_function.Image_To_Image_edit)
                {
                    API.style.display = DisplayStyle.Flex;
                    api_key_input.style.display = DisplayStyle.Flex;
                    save_api_key_button.style.display = DisplayStyle.Flex;
                    function_choice.style.display = DisplayStyle.Flex;
                    image_generation_prompt.style.display = DisplayStyle.Flex;
                    base_image_select.style.display = DisplayStyle.Flex;
                    base_image.style.display = DisplayStyle.Flex;
                    mask_image_select.style.display = DisplayStyle.Flex;
                    image_resolution.style.display = DisplayStyle.None;
                    style_field.style.display = DisplayStyle.None;
                    num_of_images.style.display = DisplayStyle.None;
                    image_size.style.display = DisplayStyle.Flex;
                    image_to_image_size.style.display = DisplayStyle.None;
                    generate_button.style.display = DisplayStyle.Flex;
                    generated_image.style.display = DisplayStyle.Flex;
                    save_button.style.display = DisplayStyle.Flex;
                }
                if (chosen_function == tool_function.Image_To_Image_Variation)
                {
                    API.style.display = DisplayStyle.Flex;
                    api_key_input.style.display = DisplayStyle.Flex;
                    save_api_key_button.style.display = DisplayStyle.Flex;
                    function_choice.style.display = DisplayStyle.Flex;
                    image_generation_prompt.style.display = DisplayStyle.None;
                    base_image_select.style.display = DisplayStyle.Flex;
                    base_image.style.display = DisplayStyle.Flex;
                    mask_image_select.style.display = DisplayStyle.None;
                    image_resolution.style.display = DisplayStyle.None;
                    style_field.style.display = DisplayStyle.None;
                    num_of_images.style.display = DisplayStyle.None;
                    image_size.style.display = DisplayStyle.Flex;
                    image_to_image_size.style.display = DisplayStyle.None;
                    generate_button.style.display = DisplayStyle.Flex;
                    generated_image.style.display = DisplayStyle.Flex;
                    save_button.style.display = DisplayStyle.Flex;
                }
            }
            else
            {
                image_resolution.style.display = DisplayStyle.None;
                num_of_images.style.display = DisplayStyle.None;
                image_size.style.display = DisplayStyle.None;
                //generate_button.style.display = DisplayStyle.None;
                //generated_image.style.display = DisplayStyle.None;
            }
        }
        
        if(help_selected == true)
        {
            API.style.display = DisplayStyle.None;
            api_key_input.style.display = DisplayStyle.None;
            save_api_key_button.style.display = DisplayStyle.None;
            function_choice.style.display = DisplayStyle.None;
            image_generation_prompt.style.display = DisplayStyle.None;
            base_image_select.style.display = DisplayStyle.None;
            base_image.style.display = DisplayStyle.None;
            mask_image_select.style.display = DisplayStyle.None;
            image_resolution.style.display = DisplayStyle.None;
            style_field.style.display = DisplayStyle.None;
            num_of_images.style.display = DisplayStyle.None;
            image_size.style.display = DisplayStyle.None;
            image_to_image_size.style.display = DisplayStyle.None;
            generate_button.style.display = DisplayStyle.None;
            generated_image.style.display = DisplayStyle.None;
            save_button.style.display = DisplayStyle.None;
        }

    }

    public void UseAPIKey()
    {
        api_key = api_key_input.value;
        //api = new OpenAIClient(api_key);
        Debug.Log(api_key);
    }
}
