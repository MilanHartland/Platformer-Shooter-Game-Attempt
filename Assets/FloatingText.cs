using UnityEngine;
using MilanUtils;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed;
    public float disappearTime;

    TextMeshProUGUI tmp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, disappearTime);
        tmp = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += moveSpeed * Time.deltaTime * Vector3.up;
        tmp.color -= new Color(0f, 0f, 0f, Time.deltaTime / disappearTime);
    }

    public static void SpawnDamageText(GameObject hitObj, float damage)
    {
        GameObject obj = Instantiate(Variables.prefabs["Damage Text"]);
        obj.transform.SetParent(GameObject.Find("World Canvas").transform);
        obj.transform.position = hitObj.transform.position + Vector3.one * Random.Range(.8f, 1f) + Vector3.right * Random.Range(-.5f, .5f);

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = damage.ToString("0.#");
        tmp.fontSize = 0.5f * Mathf.Pow(damage, .2f); //Gets the 5th root of the damage as the font size
        tmp.color = new Color(Random.Range(.7f, 1f), Random.value * .15f, Random.value * .15f);
    }

    public static void SpawnOreText(GameObject crate, int count)
    {
        GameObject obj = Instantiate(Variables.prefabs["Ore Text"]);
        obj.transform.SetParent(GameObject.Find("World Canvas").transform);
        obj.GetComponent<TextMeshProUGUI>().text = $"+{count} Ore";
        obj.transform.position = crate.transform.position;
    }
}
