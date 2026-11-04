using System.Collections;
using UnityEngine;
using TMPro;

public class FeedbackController : MonoBehaviour
{
    #region SINGLETON

    private static FeedbackController _instance;

    public static FeedbackController instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<FeedbackController>();
            return _instance;
        }
    }

    #endregion

    [SerializeField] private Animator parabensAnim; // Animator do feedback positivo
    [SerializeField] private Animator errouAnim; // Animator do feedback negativo

    [SerializeField] private TMP_Text parabensText; // Texto de parabéns
    [SerializeField] private TMP_Text errouText; // Texto de erro

    // Ativa a animação de parabéns
    public void PlayParabens()
    {

        StartCoroutine(InAnimator(parabensAnim));
    }

    // Ativa a animação de erro
    public void PlayErrou()
    {
        StartCoroutine(InAnimator(errouAnim));
    }

    // Configura texto e cor do feedback positivo
    public void SetParabens(string str, Color col)
    {
        parabensText.text = str;
        parabensText.color = col;
    }

    // Configura texto e cor do feedback negativo
    public void SetErrou(string str, Color col)
    {
        errouText.text = str;
        errouText.color = col;
    }

    // Corrotina que controla a sequencia de animação do feedback
    private IEnumerator InAnimator(Animator anim)
    {
        anim.gameObject.SetActive(true);
        anim.Play("InFeedback");

        yield return new WaitForSeconds(5f);

        anim.Play("OutFeedback");

        yield return new WaitForSeconds(.5f);

        anim.gameObject.SetActive(false);
    }
}
