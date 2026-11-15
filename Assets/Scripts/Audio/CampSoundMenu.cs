
using UnityEngine;

public class CampSoundMenu : MonoBehaviour
{
    public GameObject soundPanel;

    public void OpenSoundMenu()
    {
        soundPanel.SetActive(true);
    }

    public void CloseSoundMenu()
    {
        soundPanel.SetActive(false);
    }
}
