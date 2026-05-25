using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARController : MonoBehaviour
{
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] GameObject placementPrefab;
    [SerializeField] ParticleSystem placementParticles;

    static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

    void Update()
    {
        if (ARSession.state != ARSessionState.SessionTracking)
            return;

        bool hasInput = Input.GetMouseButtonDown(0);
        Vector2 screenPosition = Input.mousePosition;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                hasInput = true;
                screenPosition = touch.position;
            }
        }

        if (!hasInput || raycastManager == null)
            return;

        if (!raycastManager.Raycast(screenPosition, Hits, TrackableType.PlaneWithinPolygon))
            return;

        PlaceObject(Hits[0].pose);
    }

    void PlaceObject(Pose pose)
    {
        if (placementPrefab != null)
            Instantiate(placementPrefab, pose.position, pose.rotation);

        if (placementParticles != null)
        {
            ParticleSystem effect = Instantiate(placementParticles, pose.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, 6f);
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySoundEffect(SoundManager.Instance.CollectClip);

        if (UIManager.Instance != null)
            UIManager.Instance.AddScore(10);

        Debug.Log("AR object placed at " + pose.position);
    }

    public void SetPlaneDetectionEnabled(bool enabled)
    {
        if (planeManager != null)
            planeManager.enabled = enabled;
    }
}
