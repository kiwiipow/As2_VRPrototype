using System.Text;
using TMPro;
using UnityEngine;

public class RecordsBoard : MonoBehaviour
{
    [SerializeField] private TMP_Text recordsText;
    [SerializeField, Min(1)] private int maxRecordsShown = 10;
    [SerializeField] private string emptyMessage = "No records yet";
    [SerializeField] private bool showDate = true;
    [SerializeField] private Color firstPlaceColor = new Color(1f, 0.84f, 0f);

    private readonly StringBuilder builder = new StringBuilder();

    private void Awake()
    {
        if (recordsText == null)
            recordsText = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        RecordsManager.RecordsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        RecordsManager.RecordsChanged -= Refresh;
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        if (recordsText == null)
            return;

        builder.Clear();

        var records = RecordsManager.Records;
        int shown = Mathf.Min(records.Count, maxRecordsShown);

        if (shown == 0)
        {
            builder.Append(emptyMessage);
        }
        else
        {
            for (int i = 0; i < shown; i++)
            {
                ScoreRecord r = records[i];
                string line = $"{i + 1}.<pos=15%>{r.score}";
                if (showDate)
                    line += $"<pos=60%>{r.date}";

                if (i == 0)
                    builder.Append($"<color=#{ColorUtility.ToHtmlStringRGB(firstPlaceColor)}>{line}</color>");
                else
                    builder.Append(line);

                if (i < shown - 1)
                    builder.Append('\n');
            }
        }

        recordsText.text = builder.ToString();
    }
}
