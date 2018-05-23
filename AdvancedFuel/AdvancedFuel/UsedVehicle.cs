namespace AdvancedFuel
{
    public class UsedVehicle
    {
        public int Handle { get; }
        public float LeftGas { get; set; }
        public float EngineOilHealth { get; set; }
        public float TransOilHealth { get; set; }

        public UsedVehicle(int handle, float leftGas, float engineOilHealth, float transOilHealth)
        {
            this.Handle = handle;
            this.LeftGas = leftGas;
            this.EngineOilHealth = engineOilHealth;
            this.TransOilHealth = transOilHealth;
        }
    }
}