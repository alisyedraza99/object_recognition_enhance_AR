using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Listens for touch events and performs an AR raycast from the screen touch point.
/// AR raycasts will only hit detected trackables like feature points and planes.
///
/// If a raycast hits a trackable, the <see cref="placedPrefab"/> is instantiated
/// and moved to the hit position.
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class PlaceOnPlane : MonoBehaviour
{

    public ImageClassifierWrapper classifier;
    public SpawnPolyObject poly;

    string label;

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }

    private void Start()
    {
        StartCoroutine(CallClassifier());
    }

    private IEnumerator CallClassifier()
    {
        while (true)
        {
            yield return new WaitForSeconds(2);
            label = classifier.ClassifyCameraImage();
            poly.labelText.text = label;

        }
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.GetMouseButton(0))
        {
            var mousePosition = Input.mousePosition;
            touchPosition = new Vector2(mousePosition.x, mousePosition.y);
            return true;
        }

        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }

        touchPosition = default;
        return false;
    }

    void Update()
    {
        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;

        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.All))
        {
            // Raycast hits are sorted by distance, so the first one
            // will be the closest hit.

            //Set that value to spawn pose of poly to spawn api result at position
            poly.spawnPose = s_Hits[0].pose;
            //Run Classifier and spawn asset with keyword of label 
            poly.RequestAssets(label);
        }
    }

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    ARRaycastManager m_RaycastManager;
}

