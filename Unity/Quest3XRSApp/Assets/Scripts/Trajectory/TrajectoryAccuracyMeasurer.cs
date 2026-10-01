using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class TrajectoryAccuracyMeasurer : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform endEffector;
    public Transform robotBaseTransform;

    [Header("Sonuç Gösterimi (opsiyonel, Unity içinde de görmek istersen)")]
    public TMP_Text resultText;

    [Header("ROS Yayını")]
    public bool publishToRos = true;
    public string topicName = "/trajectory_accuracy";

    private ROSConnection ros;

    // Her segment için (istenen - gerçek) farkı, işaretli, metre cinsinden.
    // İşaretli tutuyoruz ki X/Y/Z'de robotun hangi yöne kaydığını da görebilelim.
    private List<Vector3> segmentErrors = new List<Vector3>();

    void Start()
    {
        if (publishToRos)
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<StringMsg>(topicName);
        }
    }

    // Yeni bir trajectory ölçümüne başlarken çağrılır - önceki verileri temizler.
    public void BeginMeasurement()
    {
        segmentErrors.Clear();
    }

    // Her segment tamamlandığında çağrılır. desiredLocalPos, o segmentin
    // hedeflediği (elle çizilen) noktanın robot tabanına göre (local) konumu.
    public void RecordSegmentResult(Vector3 desiredLocalPos)
    {
        if (endEffector == null)
        {
            Debug.LogWarning("TrajectoryAccuracyMeasurer: endEffector atanmamış, ölçüm yapılamıyor.");
            return;
        }

        Vector3 actualLocalPos = robotBaseTransform != null
            ? robotBaseTransform.InverseTransformPoint(endEffector.position)
            : endEffector.position;

        Vector3 diff = desiredLocalPos - actualLocalPos; // metre, işaretli
        segmentErrors.Add(diff);
    }

    // Trajectory tamamen bittiğinde çağrılır - toplam ve eksen bazlı sonucu
    // hesaplar, Unity Console yerine (istenirse ayrıca UI'a da) ve ROS
    // topic'ine yayınlar.
    public void FinalizeAndDisplay()
    {
        if (segmentErrors.Count == 0)
        {
            Debug.Log("TrajectoryAccuracyMeasurer: olculecek segment yok.");
            return;
        }

        float sumAbsX = 0f, sumAbsY = 0f, sumAbsZ = 0f, sumMag = 0f;
        float maxAbsX = 0f, maxAbsY = 0f, maxAbsZ = 0f, maxMag = 0f;

        foreach (var diff in segmentErrors)
        {
            float ax = Mathf.Abs(diff.x);
            float ay = Mathf.Abs(diff.y);
            float az = Mathf.Abs(diff.z);
            float mag = diff.magnitude;

            sumAbsX += ax;
            sumAbsY += ay;
            sumAbsZ += az;
            sumMag += mag;

            if (ax > maxAbsX) maxAbsX = ax;
            if (ay > maxAbsY) maxAbsY = ay;
            if (az > maxAbsZ) maxAbsZ = az;
            if (mag > maxMag) maxMag = mag;
        }

        int n = segmentErrors.Count;

        float avgX = (sumAbsX / n) * 100f; // cm
        float avgY = (sumAbsY / n) * 100f;
        float avgZ = (sumAbsZ / n) * 100f;
        float avgMag = (sumMag / n) * 100f;

        float mx = maxAbsX * 100f;
        float my = maxAbsY * 100f;
        float mz = maxAbsZ * 100f;
        float mmag = maxMag * 100f;

        string message = string.Format(
            "Trajectory Accuracy ({0} nokta) -- X: ort {1:F2} cm (maks {2:F2} cm) | Y: ort {3:F2} cm (maks {4:F2} cm) | Z: ort {5:F2} cm (maks {6:F2} cm) | Toplam sapma: ort {7:F2} cm (maks {8:F2} cm)",
            n, avgX, mx, avgY, my, avgZ, mz, avgMag, mmag);

        // Bilinçli olarak Debug.Log de birakiyoruz (Unity tarafinda da gormek
        // istersen diye), ama asil istenen cikti ROS terminalinde.
        Debug.Log(message);

        if (resultText != null)
        {
            resultText.text = message;
        }

        if (publishToRos && ros != null)
        {
            ros.Publish(topicName, new StringMsg(message));
        }
    }
}