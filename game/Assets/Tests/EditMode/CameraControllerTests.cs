using NUnit.Framework;
using UnityEngine;

public class CameraControllerTests
{
    [Test]
    public void SetPerspective_PlacesBlackOnOppositeSideOfBoard()
    {
        GameObject cameraObject = new GameObject("Camera Test");
        try
        {
            CameraController cameraController = cameraObject.AddComponent<CameraController>();

            cameraController.SetPerspective(ChessSide.White, true);
            Vector3 whitePosition = cameraObject.transform.position;

            cameraController.SetPerspective(ChessSide.Black, true);
            Vector3 blackPosition = cameraObject.transform.position;

            Assert.Less(whitePosition.z, 0f);
            Assert.Greater(blackPosition.z, 0f);
            Assert.AreEqual(whitePosition.y, blackPosition.y, 0.001f);
            Assert.AreEqual(Mathf.Abs(whitePosition.z), Mathf.Abs(blackPosition.z), 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }
}
