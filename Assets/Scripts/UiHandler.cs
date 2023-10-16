using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UiHandler : MonoBehaviour
{
    public void SceneChange(int index)
    {
        SceneManager.LoadScene(index);
    }
    
}
