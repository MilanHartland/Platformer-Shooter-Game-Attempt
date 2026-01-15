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
        obj.GetComponent<TextMeshProUGUI>().text = damage.ToString("#.#");
        obj.transform.position = hitObj.transform.position + Vector3.one * Random.Range(.8f, 1f) + Vector3.right * Random.Range(-.5f, .5f);
    }

    public static void SpawnOreText(GameObject crate, int count)
    {
        GameObject obj = Instantiate(Variables.prefabs["Ore Text"]);
        obj.transform.SetParent(GameObject.Find("World Canvas").transform);
        obj.GetComponent<TextMeshProUGUI>().text = $"+{count} Ore";
        obj.transform.position = crate.transform.position;
    }
}
