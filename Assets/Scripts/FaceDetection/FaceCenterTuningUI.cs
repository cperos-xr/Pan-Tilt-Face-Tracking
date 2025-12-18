using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Simple UI bridge to tune FaceCenterListener parameters at runtime.
public class FaceCenterTuningUI : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private FaceCenterListener listener;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField thresholdXField;
    [SerializeField] private TMP_InputField thresholdYField;
    [SerializeField] private TMP_InputField stepField;
    [SerializeField] private TMP_InputField minStepField;
    [SerializeField] private TMP_InputField maxStepField;
    [SerializeField] private Toggle allowDiagonalToggle;
    [SerializeField] private Toggle scaleByErrorToggle;

    private void OnEnable()
    {
        if (listener == null) return;

        SetField(thresholdXField, listener.thresholdX);
        SetField(thresholdYField, listener.thresholdY);
        SetField(stepField, listener.stepDegrees);
        SetField(minStepField, listener.minStepDegrees);
        SetField(maxStepField, listener.maxStepDegrees);

        if (allowDiagonalToggle != null) allowDiagonalToggle.isOn = listener.allowDiagonal;
        if (scaleByErrorToggle != null) scaleByErrorToggle.isOn = listener.scaleStepByError;

        Bind(thresholdXField, () => listener.thresholdX, v => listener.thresholdX = v);
        Bind(thresholdYField, () => listener.thresholdY, v => listener.thresholdY = v);
        Bind(stepField, () => listener.stepDegrees, v => listener.stepDegrees = v);
        Bind(minStepField, () => listener.minStepDegrees, v => listener.minStepDegrees = Mathf.Max(0f, v));
        Bind(maxStepField, () => listener.maxStepDegrees, v => listener.maxStepDegrees = Mathf.Max(listener.minStepDegrees, v));

        if (allowDiagonalToggle != null)
            allowDiagonalToggle.onValueChanged.AddListener(val => listener.allowDiagonal = val);
        if (scaleByErrorToggle != null)
            scaleByErrorToggle.onValueChanged.AddListener(val => listener.scaleStepByError = val);
    }

    private void OnDisable()
    {
        Unbind(thresholdXField);
        Unbind(thresholdYField);
        Unbind(stepField);
        Unbind(minStepField);
        Unbind(maxStepField);

        if (allowDiagonalToggle != null) allowDiagonalToggle.onValueChanged.RemoveAllListeners();
        if (scaleByErrorToggle != null) scaleByErrorToggle.onValueChanged.RemoveAllListeners();
    }

    private void Bind(TMP_InputField field, System.Func<float> getter, System.Action<float> setter)
    {
        if (field == null || setter == null) return;
        field.onEndEdit.AddListener(text =>
        {
            if (float.TryParse(text, out var val))
            {
                setter(val);
            }
            SetField(field, getter != null ? getter() : 0f);
        });
    }

    private void Unbind(TMP_InputField field)
    {
        if (field == null) return;
        field.onEndEdit.RemoveAllListeners();
    }

    private void SetField(TMP_InputField field, float value)
    {
        if (field == null) return;
        field.text = value.ToString("0.##");
    }

}
