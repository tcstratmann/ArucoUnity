﻿using UnityEngine;

namespace ArucoUnity
{
  /// \addtogroup aruco_unity_package
  /// \{

  namespace Utility
  {
    /// <summary>
    /// Manages any connected webcam to the machine, and retrieves and displays the camera's image every frame.
    /// Based on: http://answers.unity3d.com/answers/1155328/view.html
    /// </summary>
    public class CameraDevice : ArucoCamera
    {
      // Editor fields

      [SerializeField]
      [Tooltip("The id of the camera device to use.")]
      private int deviceId = 0;

      [SerializeField]
      [Tooltip("The file path to load the camera parameters.")]
      private string cameraParametersFilePath = "Assets/ArucoUnity/aruco-calibration.xml";

      [SerializeField]
      [Tooltip("Preserve the aspect ratio of the camera device's image.")]
      private bool preserveAspectRatio = true;

      // ArucoCamera properties implementation

      /// <summary>
      /// The correct image orientation.
      /// </summary>
      public override Quaternion ImageRotation
      {
        get
        {
          return Quaternion.Euler(0f, 0f, -WebCamTexture.videoRotationAngle);
        }
      }

      /// <summary>
      /// The image ratio.
      /// </summary>
      public override float ImageRatio
      {
        get
        {
          return WebCamTexture.width / (float)WebCamTexture.height;
        }
      }

      /// <summary>
      /// Allow to unflip the image if vertically flipped (use for image plane).
      /// </summary>
      public override Mesh ImageMesh
      {
        get
        {
          Mesh mesh = new Mesh();

          mesh.vertices = new Vector3[]
          {
            new Vector3(-0.5f, -0.5f, 0.0f),
            new Vector3(0.5f, 0.5f, 0.0f),
            new Vector3(0.5f, -0.5f, 0.0f),
            new Vector3(-0.5f, 0.5f, 0.0f),
          };
          mesh.triangles = new int[] { 0, 1, 2, 1, 0, 3 };

          Vector2[] defaultUv = new Vector2[]
          {
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 1.0f)
          };
          Vector2[] verticallyMirroredUv = new Vector2[]
          {
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(1.0f, 1.0f),
            new Vector2(0.0f, 0.0f)
          };
          mesh.uv = WebCamTexture.videoVerticallyMirrored ? verticallyMirroredUv : defaultUv;

          mesh.RecalculateNormals();

          return mesh;
        }
      }

      /// <summary>
      /// Allow to unflip the image if vertically flipped (use for canvas).
      /// </summary>
      public override Rect ImageUvRectFlip
      {
        get
        {
          Rect defaultRect = new Rect(0f, 0f, 1f, 1f),
               verticallyMirroredRect = new Rect(0f, 1f, 1f, -1f);
          return WebCamTexture.videoVerticallyMirrored ? verticallyMirroredRect : defaultRect;
        }
      }

      /// <summary>
      /// Mirror front-facing camera's image horizontally to look more natural.
      /// </summary>
      public override Vector3 ImageScaleFrontFacing
      {
        get
        {
          Vector3 defaultScale = new Vector3(1f, 1f, 1f),
                  frontFacingScale = new Vector3(-1f, 1f, 1f);
          return WebCamDevice.isFrontFacing ? frontFacingScale : defaultScale;
        }
      }

      // Properties

      /// <summary>
      /// The id of the camera device to use.
      /// </summary>
      public int DeviceId { get { return deviceId; } set { deviceId = value; } }

      /// <summary>
      /// The file path to load the camera parameters.
      /// </summary>
      public string CameraParametersFilePath { get { return cameraParametersFilePath; } set { cameraParametersFilePath = value; } }

      /// <summary>
      /// Preserve the aspect ratio of the camera device's image.
      /// </summary>
      public bool PreserveAspectRatio { get { return preserveAspectRatio; } set { preserveAspectRatio = value; } }

      /// <summary>
      /// The associated webcam device.
      /// </summary>
      public WebCamDevice WebCamDevice { get; protected set; }

      /// <summary>
      /// The texture of the associated webcam device.
      /// </summary>
      public WebCamTexture WebCamTexture { get; protected set; }

      /// <summary>
      /// Camera that shot the <see cref="ArucoCamera.CameraImage"/> in order to maintain the aspect ratio of 
      /// <see cref="ArucoCamera.ImageTexture"/> on screen.
      /// </summary>
      public Camera CameraBackground { get; protected set; }

      // Variables

      GameObject cameraPlane;

      // MonoBehaviour methods

      /// <summary>
      /// Once the <see cref="WebCamTexture"/> is started, update every frame the <see cref="ArucoCamera.ImageTexture"/> with the 
      /// <see cref="WebCamTexture"/> content.
      /// </summary>
      protected void Update()
      {
        if (Configured)
        {
          // Wait the WebCamTexture to be initialized, to configure the ImageTexture, the CameraPlane and notify the camera has started
          if (!Started)
          {
            if (WebCamTexture.width < 100)
            {
              Debug.Log(gameObject.name + ": Still waiting another frame for correct info.");
            }
            else
            {
              ImageTexture = new Texture2D(WebCamTexture.width, WebCamTexture.height, TextureFormat.RGB24, false);

              if (DisplayImage)
              {
                ConfigureCameraPlane();
              }

              Started = true;
              RaiseOnStarted();
            }
          }

          // Update the ImageTexture content
          if (Started)
          {
            ImageTexture.SetPixels32(WebCamTexture.GetPixels32());
          }
        }
      }

      // ArucoCamera methods

      /// <summary>
      /// Configure the camera device with the id <see cref="DeviceId"/> and its properties.
      /// </summary>
      /// <returns>If the operation has been successful.</returns>
      public override bool Configure()
      {
        if (Started)
        {
          Debug.LogError(gameObject.name + ": Stop the camera to configure it. Aborting configuration.");
          return Configured = false;
        }

        // Try to check for the camera device
        WebCamDevice[] webcamDevices = WebCamTexture.devices;
        if (webcamDevices.Length < DeviceId)
        {
          Debug.LogError(gameObject.name + ": The camera device with the id '" + DeviceId + "' is not found. Aborting configuration.");
          return Configured = false;
        }

        // Try to load the camera parameters
        CameraParameters = CameraParameters.LoadFromXmlFile(CameraParametersFilePath);
        if (CameraParameters == null)
        {
          Debug.LogError(gameObject.name + ": Couldn't load the camera parameters file path '" + CameraParametersFilePath + ".");
        }

        // Switch the camera device
        WebCamDevice = webcamDevices[DeviceId];
        WebCamTexture = new WebCamTexture(WebCamDevice.name);

        // Update state
        Configured = true;
        RaiseOnConfigured();

        // AutoStart
        if (AutoStart)
        {
          StartCamera();
        }

        return Configured;
      }

      /// <summary>
      /// Start the camera and the associated webcam device.
      /// </summary>
      public override bool StartCamera()
      {
        if (Started)
        {
          return false;
        }

        WebCamTexture.Play();
        Started = false; // Need some frames to be started, see Update()

        return true;
      }

      /// <summary>
      /// Stop the camera and the associated webcam device, and notify of the stopping.
      /// </summary>
      public override bool StopCamera()
      {
        if (!Started)
        {
          return false;
        }

        WebCamTexture.Stop();
        Started = false;
        RaiseOnStopped();

        return true;
      }

      /// <summary>
      /// Configure the <see cref="ArucoCamera.CameraImage"/>, the <see cref="CameraBackground"/> and a facing plane of the CameraImage that will 
      /// display the <see cref="ArucoCamera.ImageTexture"/>.
      /// </summary>
      // TODO: handle case of CameraParameters.ImageHeight != ImageTexture.height or CameraParameters.ImageWidth != ImageTexture.width
      // TODO: handle case of CameraParameters.FixAspectRatio != 0
      protected void ConfigureCameraPlane()
      {
        // Use the image texture's width as a default value if there is no camera parameters
        float CameraPlaneDistance = (CameraParameters != null) ? CameraParameters.CameraFy : ImageTexture.width;

        // Configure the CameraImage according to the camera parameters
        CameraImage = GetComponent<Camera>();

        float farClipPlaneNewValueFactor = 1.01f; // To be sure that the camera plane is visible by the camera
        float vFov = 2f * Mathf.Atan(0.5f * ImageTexture.height / CameraPlaneDistance) * Mathf.Rad2Deg;

        CameraImage.orthographic = false;
        CameraImage.fieldOfView = vFov;
        CameraImage.farClipPlane = CameraPlaneDistance * farClipPlaneNewValueFactor;
        CameraImage.aspect = ImageRatio;
        CameraImage.transform.position = Vector3.zero;
        CameraImage.transform.rotation = Quaternion.identity;

        // Configure the plane facing the CameraImage that display the texture
        if (cameraPlane == null)
        {
          cameraPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
          cameraPlane.name = "CameraImagePlane";
          cameraPlane.transform.parent = this.transform;
          cameraPlane.GetComponent<Renderer>().material = Resources.Load("CameraImage") as Material;
        }

        cameraPlane.transform.position = new Vector3(0, 0, CameraPlaneDistance);
        cameraPlane.transform.rotation = ImageRotation;
        cameraPlane.transform.localScale = new Vector3(ImageTexture.width, ImageTexture.height, 1);
        cameraPlane.transform.localScale = Vector3.Scale(cameraPlane.transform.localScale, ImageScaleFrontFacing);
        cameraPlane.GetComponent<MeshFilter>().mesh = ImageMesh;
        cameraPlane.GetComponent<Renderer>().material.mainTexture = ImageTexture;
        cameraPlane.SetActive(true);

        // If preserving the aspect ratio of the CameraImage, create a second camera that shot it as background
        if (PreserveAspectRatio)
        {
          if (CameraBackground == null)
          {
            GameObject CameraBackgroundGameObject = new GameObject("BlackBackgroundCamera");
            CameraBackgroundGameObject.transform.parent = this.transform;

            CameraBackground = CameraBackgroundGameObject.AddComponent<Camera>();
            CameraBackground.clearFlags = CameraClearFlags.SolidColor;
            CameraBackground.backgroundColor = Color.black;
            CameraBackground.depth = CameraImage.depth + 1; // Render after the CameraImage

            CameraBackground.orthographic = false;
            CameraBackground.fieldOfView = CameraImage.fieldOfView;
            CameraBackground.nearClipPlane = CameraImage.nearClipPlane;
            CameraBackground.farClipPlane = CameraImage.farClipPlane;
            CameraBackground.transform.position = CameraImage.transform.position;
            CameraBackground.transform.rotation = CameraImage.transform.rotation;
          }
          CameraBackground.gameObject.SetActive(true);
        }
        else if (CameraBackground != null)
        {
          CameraBackground.gameObject.SetActive(false);
        }
      }
    }
  }

  /// \} aruco_unity_package
}