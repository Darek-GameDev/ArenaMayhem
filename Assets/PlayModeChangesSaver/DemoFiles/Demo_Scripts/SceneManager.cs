using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneManager : MonoBehaviour
{
    public void LoadScene01()
    {
        SceneManager.LoadScene("Scene01");
    }
    public void LoadScene02()
    {
        SceneManager.LoadScene("Scene02");
    }
}
