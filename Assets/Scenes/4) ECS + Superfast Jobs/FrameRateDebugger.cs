using UnityEngine;
using UnityEngine.UI;

public class FrameRateDebugger : MonoBehaviour {

    [SerializeField] private Text text;
    [SerializeField] private float refreshTime = 0.5f;

    int frameCounter = 0;
    float timeCounter = 0.0f;
    float lastFramerate = 0.0f;

    private void Update() {
        if( timeCounter < refreshTime ) {
            timeCounter += Time.deltaTime;
            frameCounter++;
        }
        else {
            lastFramerate = (float)frameCounter / timeCounter;
            frameCounter = 0;
            timeCounter = 0.0f;
            
            text.text = "FPS: " + Mathf.FloorToInt(lastFramerate).ToString();
        }
    }
}
