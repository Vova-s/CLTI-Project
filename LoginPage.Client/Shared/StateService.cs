namespace LoginPage.Client.Shared
{
    public class StateService
    {
        public double KpiValue { get; private set; } = 0;
        public string PpiValue { get; set; } = "";

        public bool IsPpiVisible => ShowPpiField;
        public bool ShowPpiField { get; private set; }
        public bool ShowKpiNormal { get; private set; }
        public bool ShowKpiLow { get; private set; }

        public bool HasKpiValue => KpiValue > 0;

        public event Action? OnChange;

        public void UpdateKpiValue(double value)
        {
            KpiValue = value;

            // логіка для зміни станів
            ShowKpiNormal = value >= 0.9 && value <= 1.3;
            ShowKpiLow = value < 0.9;
            ShowPpiField = value > 1.3;

            NotifyStateChanged();
        }

        public void Reset()
        {
            KpiValue = 0;
            PpiValue = "";
            ShowKpiNormal = false;
            ShowKpiLow = false;
            ShowPpiField = false;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }

}
