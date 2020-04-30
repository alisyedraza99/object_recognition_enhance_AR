using UnityEngine;
using UnityEngine.UI;

using PolyToolkit;

/// <summary>
/// Simple example that loads and displays one asset.
/// 
/// This example requests a specific asset and displays it.
/// </summary>
public class SpawnPolyObject : MonoBehaviour
{

    // ATTENTION: Before running this example, you must set your API key in Poly Toolkit settings.
    //   1. Click "Poly | Poly Toolkit Settings..."
    //      (or select PolyToolkit/Resources/PtSettings.asset in the editor).
    //   2. Click the "Runtime" tab.
    //   3. Enter your API key in the "Api key" box.
    //
    // This example does not use authentication, so there is no need to fill in a Client ID or Client Secret.

    Pose m_SpawnPose;

    public Pose spawnPose
    {
        get { return m_SpawnPose; }
        set { m_SpawnPose = value; }
    }

    // Text where we display the current status.
    public Text statusText;
    public Text labelText;

    public void RequestAssets(string label)
    {
        // Request the asset.
        Debug.Log("Requesting asset...");
        //PolyApi.GetAsset("assets/5vbJ5vildOq", GetAssetCallback);
        PolyListAssetsRequest req = new PolyListAssetsRequest();
        // Search by keyword:
        req.keywords = label;
        // Only curated assets:
        req.curated = true;
        // Limit complexity to medium.
        req.maxComplexity = PolyMaxComplexityFilter.SIMPLE;
        // Only Blocks objects.
        req.formatFilter = null;

        req.category = PolyCategory.UNSPECIFIED;
        // Order from best to worst.
        req.orderBy = PolyOrderBy.BEST;
        // Up to 20 results per page.
        req.pageSize = 1;
        // Send the request.
        PolyApi.ListAssets(req, GetAssetCallback);
        statusText.text = "Requesting...";
        labelText.text = label;
    }

    // Callback invoked when the featured assets results are returned.
    private void GetAssetCallback(PolyStatusOr<PolyListAssetsResult> result)
    {

        if (!result.Ok)
        {

            Debug.LogError("Failed to get assets. Reason: " + result.Status);

            statusText.text = "ERROR: " + result.Status;

            return;

        }
        Debug.Log("Successfully got asset!");

        foreach (PolyAsset asset in result.Value.assets)
        {
            // Set the import options.
            PolyImportOptions options = PolyImportOptions.Default();
            // We want to rescale the imported mesh to a specific size.
            options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
            // The specific size we want assets rescaled to (fit in a 5x5x5 box):
            options.desiredSize = 0.4f;
            // We want the imported assets to be recentered such that their centroid coincides with the origin:
            options.recenter = true;

            statusText.text = "Importing...";
            PolyApi.Import(asset, options, ImportAssetCallback);
        }

    }

    // Callback invoked when an asset has just been imported.
    private void ImportAssetCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result)
    {
        if (!result.Ok)
        {
            Debug.LogError("Failed to import asset. :( Reason: " + result.Status);
            statusText.text = "ERROR: Import failed: " + result.Status;
            return;
        }

        Debug.Log("Successfully imported asset!");

        // Show attribution (asset title and author).

        statusText.text = "Asset: " + asset.displayName + "\nby " + asset.authorName;

        // Here, you would place your object where you want it in your scene, and add any
        // behaviors to it as needed by your app. As an example, let's just make it
        // slowly rotate:

        result.Value.gameObject.AddComponent<Rotate>();
        result.Value.gameObject.transform.position = spawnPose.position;
        Pose p = spawnPose;
    }
}
