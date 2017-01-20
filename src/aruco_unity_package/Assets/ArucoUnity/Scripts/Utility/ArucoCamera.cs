﻿using UnityEngine;

namespace ArucoUnity
{
  /// \addtogroup aruco_unity_package
  /// \{

  namespace Utility
  {
    public abstract class ArucoCamera : MonoBehaviour
    {
      // Events

      public delegate void ArucoCameraAction();

      /// <summary>
      /// Executed when the camera starts.
      /// </summary>
      public event ArucoCameraAction OnStarted;

      /// <summary>
      /// Executed when the camera stops.
      /// </summary>
      public event ArucoCameraAction OnStopped;

      // Properties

      /// <summary>
      /// True when the camera has started.
      /// </summary>
      public bool Started { get; protected set; }

      /// <summary>
      /// Image camera texture, updated each frame.
      /// </summary>
      public Texture2D Texture2D { get; protected set; }

      /// <summary>
      /// The parameters of the camera.
      /// </summary>
      public CameraParameters CameraParameters { get; protected set; }

      /// <summary>
      /// The correct image orientation.
      /// </summary>
      public virtual Quaternion ImageRotation { get; protected set; }

      /// <summary>
      /// The image ratio.
      /// </summary>
      public virtual float ImageRatio { get; protected set; }

      /// <summary>
      /// Allow to unflip the image if vertically flipped (use for image plane).
      /// </summary>
      public virtual Mesh ImageMesh { get; protected set; }

      /// <summary>
      /// Allow to unflip the image if vertically flipped (use for canvas).
      /// </summary>
      public virtual Rect ImageUvRectFlip { get; protected set; }

      /// <summary>
      /// Mirror front-facing camera's image horizontally to look more natural.
      /// </summary>
      public virtual Vector3 ImageScaleFrontFacing { get; protected set; }

      // Methods

      /// <summary>
      /// Populate <see cref="CameraParameters"/> from a previously saved camera parameters XML file.
      /// </summary>
      /// <param name="cameraParametersFilePath">The file path to load.</param>
      /// <returns>If the camera parameters has been successfully loaded.</returns>
      public abstract bool LoadCameraParameters(string cameraParametersFilePath);

      /// <summary>
      /// Initialize the camera and its texture.
      /// </summary>
      //public abstract void ResetCamera(); // TODO: fix

      /// <summary>
      /// Start the camera.
      /// </summary>
      public abstract void StartCamera();

      /// <summary>
      /// Stop the camera.
      /// </summary>
      public abstract void StopCamera();

      /// <summary>
      /// Execute the <see cref="OnStarted"/> action.
      /// </summary>
      protected void RaiseOnStarted()
      {
        if (OnStarted != null)
        {
          OnStarted();
        }
      }

      /// <summary>
      /// Execute the <see cref="OnStarted"/> action.
      /// </summary>
      protected void RaiseOnStopped()
      {
        if (OnStopped != null)
        {
          OnStopped();
        }
      }
    }
  }

  /// \} aruco_unity_package
}