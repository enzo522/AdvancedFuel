using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AdvancedFuel
{
    public class AdvancedFuel : Script
    {
        private static readonly float uiX;
        private static readonly float uiY;
        private static readonly int uiColor;

        private static bool started;
        private static int totalGas;
        private float leftGas;
        private static int maxEngOilHealth;
        private float engineOilHealth;
        private static int maxTransOilHealth;
        private float transOilHealth;

        private List<UsedVehicle> usedVehicles;
        private static float gasPrice;
        private static float totalPrice;
        private float refuelAmount;
        private float remainder;
        private Menu menu;
        private float menuY;
        private int clock;
        
        private bool drawStationUI;
        private bool doesEngineHaveToStop;
        private bool doesOilHaveToBeRemoved;
        private bool forTires;
        private bool isDoingSomething;

        private static List<Blip> gasStationBlips;
        private static List<Blip> planeStationBlips;
        private static List<Blip> heliStationBlips;
        private static List<Blip> boatStationBlips;

        private static Random r;
        private static Ped playerPed;
        private static Vehicle playerVehicle;

        public enum Menu
        {
            Fuel,
            EngineOil,
            TransOil,
            Repair,
            BulletproofTire,
            HeliumHeadlight
        }

        static AdvancedFuel()
        {
            uiX = 0.0855f;
            uiY = 0.802f;
            uiColor = 100;

            started = true;
            totalGas = 0;
            maxEngOilHealth = 0;
            maxTransOilHealth = 0;
            
            gasPrice = 0.0f;
            totalPrice = 0.0f;

            r = new Random();
            playerPed = null;
            playerVehicle = null;

            SettingStations();
        }

        public AdvancedFuel()
        {
            usedVehicles = new List<UsedVehicle>();
            leftGas = 0.0f;
            engineOilHealth = 0.0f;
            transOilHealth = 0.0f;

            refuelAmount = 0.0f;
            remainder = 0.0f;
            menu = Menu.Fuel;
            menuY = 0.0f;
            clock = 0;
            
            drawStationUI = false;
            doesEngineHaveToStop = false;
            doesOilHaveToBeRemoved = false;
            forTires = false;
            isDoingSomething = false;

            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(Object sender, EventArgs e)
        {
            DrawText(currentTime(), uiX + 0.058f, uiY + 0.143f, 0.38f, 1);
            playerPed = Game.Player.Character;
            
            if (Function.Call<bool>(Hash.GET_MISSION_FLAG)) started = false;
            else
            {
                if (playerPed.IsInVehicle())
                {
                    playerVehicle = playerPed.CurrentVehicle;

                    if (playerVehicle.Model.IsBicycle) started = false;
                    else started = true;
                }
                else
                {
                    started = false;
                    playerVehicle = null;

                    if (playerPed.IsGettingIntoAVehicle)
                    {
                        UsedVehicle uv = usedVehicles.Find(v => v.Handle == playerPed.GetVehicleIsTryingToEnter().Handle);

                        if (uv != null)
                        {
                            SettingValues(playerPed.GetVehicleIsTryingToEnter());
                            leftGas = uv.LeftGas;
                            engineOilHealth = uv.EngineOilHealth;
                            transOilHealth = uv.TransOilHealth;
                        }
                        else
                        {
                            SettingValues(playerPed.GetVehicleIsTryingToEnter());
                            leftGas = r.Next(5, totalGas);
                            engineOilHealth = r.Next(100, maxEngOilHealth);
                            transOilHealth = r.Next(100, maxTransOilHealth);
                            usedVehicles.Add(new UsedVehicle(playerPed.GetVehicleIsTryingToEnter().Handle, leftGas, engineOilHealth, transOilHealth));
                        }
                    }
                }
            }

            if (started)
            {
                UsedVehicle uv = usedVehicles.Find(v => v.Handle == playerVehicle.Handle);

                if (uv != null)
                {
                    uv.LeftGas = leftGas;
                    uv.EngineOilHealth = engineOilHealth;
                    uv.TransOilHealth = transOilHealth;
                }

                if (playerVehicle.Speed > 0)
                {
                    leftGas -= playerVehicle.Speed / 24000;
                    engineOilHealth -= playerVehicle.Speed / 24000;
                    transOilHealth -= playerVehicle.Speed / 24000;

                    if (engineOilHealth <= maxEngOilHealth * 0.2) playerVehicle.EngineHealth -= 0.02f;
                    if (transOilHealth <= maxTransOilHealth * 0.2) playerVehicle.EngineHealth -= 0.02f;
                }

                if (leftGas <= 0)
                {
                    leftGas = 0.0f;
                    playerVehicle.IsDriveable = false;
                    UI.Notify("Your gas tank is empty.");
                }

                if (engineOilHealth <= 0) engineOilHealth = 0.0f;
                if (transOilHealth <= 0) transOilHealth = 0.0f;

                Function.Call(Hash.DRAW_RECT, uiX, uiY, 0.1405f, 0.028f, 0, 0, 0, 150);
                Function.Call(Hash.DRAW_RECT, uiX + 0.061f, uiY, 0.016f, 0.026f, uiColor, uiColor, uiColor, 70);
                DrawText(Convert.ToString(Math.Floor(Convert.ToDouble(playerVehicle.Speed) * 4)), uiX + 0.061f, uiY - 0.017f, 0.38f, 1);
                DrawText("KM/H", uiX + 0.061f, uiY + 0.001f, 0.21f, 1);
                Function.Call(Hash.DRAW_RECT, uiX + 0.029f, uiY + 0.003f, 0.045f, 0.021f, uiColor, uiColor, uiColor, 70);

                if (AdvancedNitro.AdvancedNitro.myNitroSystemEnabled)
                {
                    if (AdvancedNitro.AdvancedNitro.myNitroAmount > 30) Function.Call(Hash.DRAW_RECT, uiX + 0.0507f - (AdvancedNitro.AdvancedNitro.myNitroAmount * 0.0002f), uiY - 0.011f, AdvancedNitro.AdvancedNitro.myNitroAmount * 0.0004f + 0.0014f, 0.004f, uiColor, uiColor, uiColor, 180);
                    else Function.Call(Hash.DRAW_RECT, uiX + 0.0507f - (AdvancedNitro.AdvancedNitro.myNitroAmount * 0.0002f), uiY - 0.01117f, AdvancedNitro.AdvancedNitro.myNitroAmount * 0.0004f + 0.0014f, 0.004f, 180, 0, 0, 180);
                }

                if (playerVehicle.CurrentRPM < 0.9f) Function.Call(Hash.DRAW_RECT, uiX + 0.006f - (playerVehicle.CurrentRPM * 0.038f), uiY - 0.004f, (playerVehicle.CurrentRPM * 0.076f), 0.007f, uiColor, uiColor, uiColor, 180);
                else Function.Call(Hash.DRAW_RECT, uiX + 0.006f - (playerVehicle.CurrentRPM * 0.038f), uiY - 0.00417f, (playerVehicle.CurrentRPM * 0.076f), 0.007f, 180, 0, 0, 180);

                DrawText("F " + Convert.ToString(Math.Round(Convert.ToDouble(leftGas / 5), 1)) + "L/" + Convert.ToString(totalGas / 5) + "L", uiX - 0.0078f, uiY - 0.001f, 0.23f, 1);

                if (leftGas / totalGas > 0.1f) Function.Call(Hash.DRAW_RECT, uiX - 0.0078f, uiY + 0.007f, 0.0273f, 0.012f, uiColor, uiColor, uiColor, 180);
                else Function.Call(Hash.DRAW_RECT, uiX - 0.0078f, uiY + 0.00717f, 0.0273f, 0.012f, 180, 0, 0, 180);

                DrawText("E " + Convert.ToString(Math.Round(Convert.ToDouble(engineOilHealth * 100 / maxEngOilHealth))) + "%", uiX - 0.0298f, uiY - 0.001f, 0.23f, 1);

                if (engineOilHealth > maxEngOilHealth * 0.3f) Function.Call(Hash.DRAW_RECT, uiX - 0.0298f, uiY + 0.007f, 0.0155f, 0.012f, uiColor, uiColor, uiColor, 180);
                else Function.Call(Hash.DRAW_RECT, uiX - 0.0298f, uiY + 0.00717f, 0.0155f, 0.012f, 180, 0, 0, 180);

                DrawText("T " + Convert.ToString(Math.Round(Convert.ToDouble(transOilHealth * 100 / maxTransOilHealth))) + "%", uiX - 0.0459f, uiY - 0.001f, 0.23f, 1);

                if (transOilHealth > maxTransOilHealth * 0.3f) Function.Call(Hash.DRAW_RECT, uiX - 0.0459f, uiY + 0.007f, 0.0155f, 0.012f, uiColor, uiColor, uiColor, 180);
                else Function.Call(Hash.DRAW_RECT, uiX - 0.0459f, uiY + 0.00717f, 0.0155f, 0.012f, 180, 0, 0, 180);

                if ((playerVehicle.BodyHealth + playerVehicle.EngineHealth) > 2000) DrawText("D 0%", uiX - 0.062f, uiY - 0.001f, 0.23f, 1);
                else if ((playerVehicle.BodyHealth + playerVehicle.EngineHealth) > 0) DrawText("D " + Convert.ToString(100 - Math.Round(Convert.ToDouble((playerVehicle.BodyHealth + playerVehicle.EngineHealth) / 20 + 0.5f))) + "%", uiX - 0.062f, uiY - 0.001f, 0.23f, 1);
                else DrawText("D 100%", uiX - 0.062f, uiY - 0.001f, 0.23f, 1);

                if ((playerVehicle.BodyHealth + playerVehicle.EngineHealth) / 2 > 500) Function.Call(Hash.DRAW_RECT, uiX - 0.062f, uiY + 0.007f, 0.0155f, 0.012f, uiColor, uiColor, uiColor, 180);
                else Function.Call(Hash.DRAW_RECT, uiX - 0.062f, uiY + 0.00717f, 0.0155f, 0.012f, 180, 0, 0, 180);

                if (!playerVehicle.ClassType.ToString().Equals("SportsClassics")) DrawText(playerVehicle.ClassType.ToString().ToUpper(), uiX + 0.029f, uiY - 0.01f, 0.35f, 1);
                else DrawText("CLASSICS", uiX + 0.029f, uiY - 0.01f, 0.35f, 1);

                if (drawStationUI)
                {
                    totalPrice = gasPrice * refuelAmount;
                    DrawText("$" + gasPrice + " / L", 0.148f, 0.18f, 0.42f, 2);

                    if (playerVehicle.Model.IsCar) DrawText("PREMIUM PETROL", 0.1f, 0.106f, 0.51f, 1);
                    else if (playerVehicle.Model.IsBike || playerVehicle.Model.IsQuadbike) DrawText("2 TAK", 0.1f, 0.106f, 0.51f, 1);
                    else if (playerVehicle.Model.IsPlane) DrawText("AVTUR", 0.1f, 0.106f, 0.51f, 1);
                    else if (playerVehicle.Model.IsHelicopter) DrawText("AVGAS", 0.1f, 0.106f, 0.51f, 1);
                    else if (playerVehicle.Model.IsBoat) DrawText("MINYAK TANAH", 0.1f, 0.106f, 0.51f, 1);

                    DrawText("S M", 0.081f, 0.003f, 0.925f, 4);
                    DrawText("A R T", 0.116f, 0.003f, 0.925f, 3);
                    DrawText("FUEL MOD", 0.083f, 0.047f, 0.4f, 2);
                    Function.Call(Hash.DRAW_RECT, 0.1f, 0.056f, 0.175f, 0.113f, uiColor, uiColor, uiColor, 125);
                    Function.Call(Hash.DRAW_RECT, 0.1f, 0.052f, 0.16f, 0.09f, 255, 255, 255, 150);
                    Function.Call(Hash.DRAW_RECT, 0.139f, 0.044f, 0.083f, 0.055f, 220, 0, 0, 255);

                    Function.Call(Hash.DRAW_RECT, 0.1f, 0.246f, 0.18f, 0.492f, uiColor, uiColor, uiColor, 125);
                    Function.Call(Hash.DRAW_RECT, 0.1f, 0.124f, 0.16f, 0.04f, uiColor, uiColor, uiColor, 125);
                    Function.Call(Hash.DRAW_RECT, 0.1f, 0.272f, 0.16f, 0.246f, 255, 255, 255, 150);

                    if (!Function.Call<string>(Hash._GET_LABEL_TEXT, playerVehicle.DisplayName).Equals("NULL")) DrawText("[ " + Function.Call<string>(Hash._GET_LABEL_TEXT, playerVehicle.DisplayName) + " ]", 0.1f, 0.15f, 0.42f, 2);
                    else DrawText("[ " + playerVehicle.DisplayName.ToUpper() + " ]", 0.1f, 0.15f, 0.42f, 2);

                    DrawText("Fuel", 0.032f, 0.21f, 0.42f, 5);
                    DrawText(Convert.ToString(Math.Round(refuelAmount, 1)), 0.148f, 0.205f, 0.42f, 2);
                    Function.Call(Hash.DRAW_RECT, 0.148f, 0.221f, 0.033f, 0.028f, uiColor, uiColor, uiColor, 200);
                    Function.Call(Hash.DRAW_RECT, 0.148f, 0.221f, 0.029f, 0.022f, 255, 255, 255, 150);

                    DrawText("Engine Oil", 0.032f, 0.24f, 0.42f, 5);
                    Function.Call(Hash.DRAW_RECT, 0.148f, 0.255f, 0.008f, 0.015f, uiColor, uiColor, uiColor, 200);

                    DrawText("Transmission Oil", 0.032f, 0.27f, 0.42f, 5);
                    Function.Call(Hash.DRAW_RECT, 0.148f, 0.285f, 0.008f, 0.015f, uiColor, uiColor, uiColor, 200);

                    DrawText("Repair Vehicle", 0.032f, 0.3f, 0.42f, 5);
                    Function.Call(Hash.DRAW_RECT, 0.148f, 0.315f, 0.008f, 0.015f, uiColor, uiColor, uiColor, 200);

                    DrawText("Buy Bulletproof Tires", 0.032f, 0.33f, 0.42f, 5);
                    Function.Call(Hash.DRAW_RECT, 0.148f, 0.345f, 0.008f, 0.015f, uiColor, uiColor, uiColor, 200);

                    DrawText("Install Helium Headlights", 0.032f, 0.36f, 0.42f, 5);
                    Function.Call(Hash.DRAW_RECT, 0.148f, 0.375f, 0.008f, 0.015f, uiColor, uiColor, uiColor, 200);

                    Function.Call(Hash.DRAW_RECT, 0.1f, 0.411f, 0.16f, 0.026f, 255, 255, 255, 60);
                    Function.Call(Hash.DRAW_RECT, 0.1f, 0.446f, 0.075f, 0.03f, uiColor, uiColor, uiColor, 200);
                    Function.Call(Hash.DRAW_RECT, 0.1f, 0.446f, 0.07f, 0.024f, 255, 255, 255, 150);
                    Function.Call(Hash.DRAW_RECT, 0.03f, menuY, 0.003f, 0.018f, uiColor, uiColor, uiColor, 200);

                    switch (menu)
                    {
                        case Menu.Fuel:
                            menuY = 0.224f;
                            DrawText("$" + Math.Round(totalPrice, 1), 0.1f, 0.43f, 0.45f, 2);
                            break;

                        case Menu.EngineOil:
                            menuY = 0.254f;
                            DrawText("$" + (maxEngOilHealth / 2), 0.1f, 0.43f, 0.45f, 2);
                            break;

                        case Menu.TransOil:
                            menuY = 0.284f;
                            DrawText("$" + (maxTransOilHealth / 2), 0.1f, 0.43f, 0.45f, 2);
                            break;

                        case Menu.Repair:
                            menuY = 0.314f;
                            DrawText("$" + Math.Round(Convert.ToDouble(2000.0f - playerVehicle.EngineHealth - playerVehicle.BodyHealth), 1), 0.1f, 0.43f, 0.45f, 2);
                            break;

                        case Menu.BulletproofTire:
                            menuY = 0.344f;
                            DrawText("$300", 0.1f, 0.43f, 0.45f, 2);
                            break;

                        case Menu.HeliumHeadlight:
                            menuY = 0.374f;
                            DrawText("$500", 0.1f, 0.43f, 0.45f, 2);
                            break;
                    }

                    if (doesEngineHaveToStop)
                    {
                        switch (menu)
                        {
                            case Menu.Fuel:
                                if (remainder >= 0)
                                {
                                    leftGas += 0.1f;
                                    remainder -= 0.02f;
                                    DrawText("Refueling.. (" + Math.Floor(Convert.ToDouble(100 * (refuelAmount - remainder) / refuelAmount)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                }
                                else
                                {
                                    if (leftGas > totalGas) leftGas = totalGas;

                                    Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                    refuelAmount = 0.0f;
                                    remainder = 0.0f;
                                    doesEngineHaveToStop = false;
                                    isDoingSomething = false;
                                }

                                break;

                            case Menu.EngineOil:
                                if (doesOilHaveToBeRemoved)
                                {
                                    if (engineOilHealth >= 1.8f)
                                    {
                                        engineOilHealth -= 1.8f;
                                        DrawText("Now removing used oil.. (" + Math.Round(Convert.ToDouble(100 * engineOilHealth / maxEngOilHealth)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                    }
                                    else
                                    {
                                        engineOilHealth = 0.0f;
                                        doesOilHaveToBeRemoved = false;
                                    }
                                }
                                else
                                {
                                    if (engineOilHealth <= (maxEngOilHealth - 1.8f))
                                    {

                                        engineOilHealth += 1.8f;
                                        DrawText("Now refilling new oil.. (" + Math.Round(Convert.ToDouble(100 * engineOilHealth / maxEngOilHealth)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                    }
                                    else
                                    {
                                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        engineOilHealth = maxEngOilHealth;
                                        doesEngineHaveToStop = false;
                                        isDoingSomething = false;
                                    }
                                }

                                break;

                            case Menu.TransOil:
                                if (doesOilHaveToBeRemoved)
                                {
                                    if (transOilHealth >= 1.8f)
                                    {
                                        transOilHealth -= 1.8f;
                                        DrawText("Now removing used oil.. (" + Math.Round(Convert.ToDouble(100 * transOilHealth / maxTransOilHealth)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                    }
                                    else
                                    {
                                        transOilHealth = 0.0f;
                                        doesOilHaveToBeRemoved = false;
                                    }
                                }
                                else
                                {
                                    if (transOilHealth <= (maxTransOilHealth - 1.8f))
                                    {
                                        transOilHealth += 1.8f;
                                        DrawText("Now refilling new oil.. (" + Math.Round(Convert.ToDouble(100 * transOilHealth / maxTransOilHealth)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                    }
                                    else
                                    {
                                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        transOilHealth = maxTransOilHealth;
                                        doesEngineHaveToStop = false;
                                        isDoingSomething = false;
                                    }
                                }

                                break;

                            case Menu.Repair:
                                if (playerVehicle.EngineHealth <= 999.0f)
                                {
                                    playerVehicle.EngineHealth += 1.0f;
                                    DrawText("Now repairing your vehicle.. (" + Math.Floor(Convert.ToDouble(100 * (playerVehicle.EngineHealth + playerVehicle.BodyHealth) / 2000)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                }
                                else if (playerVehicle.BodyHealth <= 999.0f)
                                {
                                    playerVehicle.BodyHealth += 1.0f;
                                    DrawText("Now repairing your vehicle.. (" + Math.Floor(Convert.ToDouble(100 * (playerVehicle.EngineHealth + playerVehicle.BodyHealth) / 2000)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                }
                                else
                                {
                                    Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                    playerVehicle.Repair();
                                    playerVehicle.BodyHealth = 1000;
                                    playerVehicle.EngineHealth = 1000;
                                    doesEngineHaveToStop = false;
                                    isDoingSomething = false;
                                }

                                break;

                            case Menu.BulletproofTire:
                                if (forTires)
                                {
                                    if (clock < 500)
                                    {
                                        DrawText("Now removing current tires.. (" + Math.Floor(Convert.ToDouble(100 * clock / 500)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                        clock++;
                                    }
                                    else if (clock == 500)
                                    {
                                        playerVehicle.BurstTire(0);
                                        playerVehicle.BurstTire(1);
                                        playerVehicle.BurstTire(2);
                                        playerVehicle.BurstTire(3);
                                        playerVehicle.BurstTire(4);
                                        playerVehicle.BurstTire(5);
                                        clock++;
                                    }
                                    else if (clock < 1000)
                                    {
                                        DrawText("Now installing bulletproof tires.. (" + Math.Floor(Convert.ToDouble(100 * (clock - 500) / 500)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                        clock++;
                                    }
                                    else if (clock >= 1000)
                                    {
                                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        playerVehicle.FixTire(0);
                                        playerVehicle.FixTire(1);
                                        playerVehicle.FixTire(2);
                                        playerVehicle.FixTire(3);
                                        playerVehicle.FixTire(4);
                                        playerVehicle.FixTire(5);

                                        playerVehicle.CanTiresBurst = false;
                                        forTires = false;
                                        doesEngineHaveToStop = false;
                                        isDoingSomething = false;
                                    }
                                }

                                break;

                            case Menu.HeliumHeadlight:
                                if (!forTires)
                                {
                                    if (clock < 500)
                                    {
                                        DrawText("Now removing current headlights.. (" + Math.Floor(Convert.ToDouble(100 * clock / 500)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                        clock++;
                                    }
                                    else if (clock < 1000)
                                    {
                                        DrawText("Now installing Helium Headlights.. (" + Math.Floor(Convert.ToDouble(100 * (clock - 500) / 500)) + "%)", 0.1f, 0.396f, 0.42f, 1);
                                        clock++;
                                    }
                                    else if (clock >= 1000)
                                    {
                                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        playerVehicle.LightsMultiplier = 18.0f;
                                        doesEngineHaveToStop = false;
                                        isDoingSomething = false;
                                    }
                                }

                                break;
                        }
                    }
                }
            }
        }

        private void OnKeyDown(Object sender, KeyEventArgs e)
        {
            if (started)
            {
                if (drawStationUI && !isDoingSomething)
                {
                    if (e.KeyCode == Keys.I)
                    {
                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        menu--;

                        if ((int)menu < (int)Menu.Fuel) menu = Menu.HeliumHeadlight;
                    }

                    if (e.KeyCode == Keys.K)
                    {
                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        menu++;

                        if ((int)menu > (int)Menu.HeliumHeadlight) menu = Menu.Fuel;
                    }

                    if (e.KeyCode == Keys.J)
                    {
                        if (menu == Menu.Fuel)
                        {
                            Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            refuelAmount--;

                            if (refuelAmount < 0) refuelAmount = 0.0f;
                        }
                    }

                    if (e.KeyCode == Keys.L)
                    {
                        if (menu == Menu.Fuel)
                        {
                            Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            refuelAmount++;

                            if (refuelAmount > ((totalGas - leftGas) / 5)) refuelAmount = (totalGas - leftGas) / 5;
                        }
                    }

                    if (e.KeyCode == Keys.E)
                    {
                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");

                        switch (menu)
                        {
                            case Menu.Fuel:
                                if (refuelAmount > 0)
                                {
                                    if (Game.Player.Money >= totalPrice)
                                    {
                                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        Game.Player.Money -= Convert.ToInt32(Math.Floor(totalPrice));
                                        remainder = refuelAmount;
                                        doesEngineHaveToStop = true;
                                        isDoingSomething = true;
                                    }
                                    else UI.Notify("You don't have enough money to refuel gas.");
                                }

                                break;

                            case Menu.EngineOil:
                                if (Game.Player.Money >= (maxEngOilHealth / 2))
                                {
                                    Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                    Game.Player.Money -= Convert.ToInt32(Math.Floor(Convert.ToDouble(maxEngOilHealth / 2)));
                                    doesEngineHaveToStop = true;
                                    doesOilHaveToBeRemoved = true;
                                    isDoingSomething = true;
                                }
                                else UI.Notify("You don't have enough money to refuel Engine Oil.");

                                break;

                            case Menu.TransOil:
                                if (Game.Player.Money >= (maxTransOilHealth / 2))
                                {
                                    Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                    Game.Player.Money -= Convert.ToInt32(Math.Floor(Convert.ToDouble(maxTransOilHealth / 2)));
                                    doesEngineHaveToStop = true;
                                    doesOilHaveToBeRemoved = true;
                                    isDoingSomething = true;
                                }
                                else UI.Notify("You don't have enough money to refuel Transmission Oil.");

                                break;

                            case Menu.Repair:
                                if (playerVehicle.EngineHealth < 1000 || playerVehicle.BodyHealth < 1000)
                                {
                                    if (Game.Player.Money >= Math.Round(Convert.ToDouble(2000.0f - playerVehicle.EngineHealth - playerVehicle.BodyHealth)))
                                    {
                                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        Game.Player.Money -= Convert.ToInt32(Math.Floor(Convert.ToDouble(2000.0f - playerVehicle.EngineHealth - playerVehicle.BodyHealth)));
                                        doesEngineHaveToStop = true;
                                        isDoingSomething = true;
                                    }
                                    else UI.Notify("You don't have enough money to repair your vehicle.");
                                }
                                else UI.Notify("No need to repair your vehicle.");

                                break;

                            case Menu.BulletproofTire:
                                if (playerVehicle.CanTiresBurst)
                                {
                                    if (playerVehicle.EngineHealth >= 1000 && playerVehicle.BodyHealth >= 1000)
                                    {
                                        if (Game.Player.Money >= 500)
                                        {
                                            Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                            Game.Player.Money -= 500;
                                            forTires = true;
                                            doesEngineHaveToStop = true;
                                            isDoingSomething = true;
                                            clock = 0;
                                        }
                                        else UI.Notify("You don't have enough money to replace tires.");
                                    }
                                    else UI.Notify("You have to repair your vehicle before replacing tires.");
                                }
                                else UI.Notify("Your vehicle already has bulletproof tires.");

                                break;

                            case Menu.HeliumHeadlight:
                                if (playerVehicle.EngineHealth >= 1000 && playerVehicle.BodyHealth >= 1000)
                                {
                                    if (Game.Player.Money >= 500)
                                    {
                                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                        Game.Player.Money -= 500;
                                        forTires = false;
                                        doesEngineHaveToStop = true;
                                        isDoingSomething = true;
                                        clock = 0;
                                    }
                                    else UI.Notify("You don't have enough money to install Helium Headlights.");
                                }
                                else UI.Notify("You have to repair your vehicle before installing Helium Headlights.");

                                break;
                        }
                    }

                    if (e.KeyCode == Keys.Q && !doesEngineHaveToStop)
                    {
                        Audio.PlaySoundFrontend("PICK_UP", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        UI.Notify("Thank you. See you later.");

                        menu = Menu.Fuel;
                        refuelAmount = 0.0f;
                        remainder = 0.0f;
                        drawStationUI = false;
                        playerVehicle.IsDriveable = true;
                        playerVehicle.EngineRunning = true;
                    }
                }
                else
                {
                    if (e.KeyCode == Keys.E)
                    {
                        if (playerVehicle.Model.IsCar || playerVehicle.Model.IsBike || playerVehicle.Model.IsQuadbike)
                        {
                            if (IsNearStation(gasStationBlips))
                            {
                                drawStationUI = true;
                                playerVehicle.EngineRunning = false;
                                playerVehicle.IsDriveable = false;
                            }
                        }
                        else if (playerVehicle.Model.IsPlane)
                        {
                            if (IsNearStation(planeStationBlips))
                            {
                                drawStationUI = true;
                                playerVehicle.EngineRunning = false;
                                playerVehicle.IsDriveable = false;
                            }
                        }
                        else if (playerVehicle.Model.IsHelicopter)
                        {
                            if (IsNearStation(heliStationBlips))
                            {
                                drawStationUI = true;
                                playerVehicle.EngineRunning = false;
                                playerVehicle.IsDriveable = false;
                            }
                        }
                        else if (playerVehicle.Model.IsBoat)
                        {
                            if (IsNearStation(boatStationBlips))
                            {
                                drawStationUI = true;
                                playerVehicle.EngineRunning = false;
                                playerVehicle.IsDriveable = false;
                            }
                        }
                    }
                }
            }
        }

        private static void SettingStations()
        {
            gasStationBlips = new List<Blip>
            {
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -80.7f, -1761.8f, 29.8f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -518.8f, -1210.0f, 18.33f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -714.85f, -932.65f, 19.2f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 273.25f, -1261.05f, 29.3f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 811.285f, -1030.9f, 26.4f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1212.5f, -1403.6f, 35.38f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 2574.15f, 359.1f, 108.5f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1183.0f, -320.42f, 69.3f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 629.05f, 274.0f, 103.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -1429.65f, -279.35f, 46.3f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -2087.57f, -321.15f, 13.1f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -1796.55f, 811.7f, 138.7f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -2558.05f, 2327.3f, 33.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 48.188f, 2779.2f, 58.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 263.0f, 2607.35f, 50.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1209.9f, 2658.7f, 37.9f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 2539.25f, 2594.6f, 37.9f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 2681.7f, 3266.4f, 55.2f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 2009.1f, 3777.6f, 32.4f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1684.1f, 4932.15f, 42.2f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1705.5f, 6414.05f, 32.7f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 171.62f, 6603.35f, 32.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -91.7f, 6423.2f, 31.6f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1043.25f, 2668.5f, 39.7f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -594.2f, 5025.4f, 140.3f)
            };

            planeStationBlips = new List<Blip>
            {
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1411.75f, 3012.3f, 41.1f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -981.0f, -2995.0f, 13.1f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 2119.5f, 4806.3f, 41.2f)
            };

            heliStationBlips = new List<Blip>
            {
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1705.7f, 3271.9f, 40.6f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 2102.05f, 4769.4f, 40.7f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -231.25f, 6257.9f, 31.2f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -1718.1f, -1008.8f, 5.2f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -1223.6f, -1824.75f, 2.2f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 846.6f, -3219.5f, 5.6f)
            };

            boatStationBlips = new List<Blip>
            {
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -1800.5f, -1233.1f, 0.3f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -3426.5f, 948.2f, 0.1f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -2527.9f, 2541.0f, 0.1f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -2302.3f, 2561.2f, 0.1f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -2076.9f, 2597.7f, 0.1f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -1768.9f, 2634.0f, 0.1f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 860.2f, 3699.8f, 30.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1731.7f, 3987.5f, 30.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 2378.7f, 4295.9f, 30.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 1298.6f, 4208.5f, 30.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 710.9f, 4090.5f, 30.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -1617.8f, 5268.5f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -304.5f, 6659.9f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 3874.2f, 4464.0f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 36.8f, -2775.9f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -60.3f, -2768.7f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -292.2f, -2761.8f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -352.9f, -2410.8f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, 128.9f, -2272.9f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -855.3f, -1486.4f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -911.7f, -1469.9f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -886.0f, -1406.8f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -964.2f, -1387.1f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -839.8f, -1380.7f, 1.0f),
                Function.Call<Blip>(Hash.ADD_BLIP_FOR_COORD, -765.8f, -1378.9f, 1.0f)
            };

            foreach (Blip b in gasStationBlips)
            {
                b.Sprite = BlipSprite.JerryCan;
                b.Color = BlipColor.White;
                b.Scale = 0.7f;
                b.IsShortRange = true;
                b.Name = "Gas Station";
            }

            foreach (Blip b in planeStationBlips)
            {
                b.Sprite = BlipSprite.Plane;
                b.Color = BlipColor.White;
                b.Scale = 0.7f;
                b.IsShortRange = true;
                b.Name = "Plane Station";
            }

            foreach (Blip b in heliStationBlips)
            {
                b.Sprite = BlipSprite.Helicopter;
                b.Color = BlipColor.White;
                b.Scale = 0.7f;
                b.IsShortRange = true;
                b.Name = "Helicopter Station";
            }

            foreach (Blip b in boatStationBlips)
            {
                b.Sprite = BlipSprite.Boat;
                b.Color = BlipColor.White;
                b.Scale = 0.7f;
                b.IsShortRange = true;
                b.Name = "Boat Station";
            }
        }

        private static void DrawText(String text, float x, float y, float scale, int version)
        {
            Function.Call(Hash.SET_TEXT_FONT, 6);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call(Hash.SET_TEXT_WRAP, 0.0f, 1.0f);

            switch (version)
            {
                case 1:
                    Function.Call(Hash.SET_TEXT_COLOUR, 240, 240, 240, 200);
                    Function.Call(Hash.SET_TEXT_CENTRE, true);
                    Function.Call(Hash.SET_TEXT_DROPSHADOW, 2, 2, 0, 0, 0);
                    Function.Call(Hash.SET_TEXT_EDGE, 1, 255, 255, 255, 205);
                    break;

                case 2:
                    Function.Call(Hash.SET_TEXT_COLOUR, 0, 0, 0, 200);
                    Function.Call(Hash.SET_TEXT_CENTRE, true);
                    Function.Call(Hash.SET_TEXT_EDGE, 2, 255, 255, 255, 205);
                    break;

                case 3:
                    Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 255);
                    Function.Call(Hash.SET_TEXT_CENTRE, true);
                    Function.Call(Hash.SET_TEXT_EDGE, 1, 0, 0, 0, 205);
                    break;

                case 4:
                    Function.Call(Hash.SET_TEXT_COLOUR, 220, 0, 0, 255);
                    Function.Call(Hash.SET_TEXT_CENTRE, true);
                    Function.Call(Hash.SET_TEXT_EDGE, 1, 0, 0, 0, 205);
                    break;

                case 5:
                    Function.Call(Hash.SET_TEXT_COLOUR, 0, 0, 0, 200);
                    Function.Call(Hash.SET_TEXT_CENTRE, false);
                    Function.Call(Hash.SET_TEXT_EDGE, 1, 0, 0, 0, 205);
                    break;
            }

            Function.Call(Hash._SET_TEXT_ENTRY, "STRING");
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, text);
            Function.Call(Hash._DRAW_TEXT, x, y);
        }

        private void SettingValues(Vehicle v)
        {
            if (v.Model.IsCar)
            {
                gasPrice = 1.7f;

                if (v.ClassType.Equals(VehicleClass.Super))
                {
                    totalGas = 380;
                    maxEngOilHealth = 750;
                    maxTransOilHealth = 850;
                }
                else if (v.ClassType.Equals(VehicleClass.SportsClassics))
                {
                    totalGas = 300;
                    maxEngOilHealth = 650;
                    maxTransOilHealth = 760;
                }
                else if (v.ClassType.Equals(VehicleClass.Sports))
                {
                    totalGas = 280;
                    maxEngOilHealth = 640;
                    maxTransOilHealth = 740;
                }
                else if (v.ClassType.Equals(VehicleClass.Coupes))
                {
                    totalGas = 250;
                    maxEngOilHealth = 590;
                    maxTransOilHealth = 600;
                }
                else if (v.ClassType.Equals(VehicleClass.Military))
                {
                    totalGas = 520;
                    maxEngOilHealth = 1000;
                    maxTransOilHealth = 1100;
                }
                else if (v.ClassType.Equals(VehicleClass.Sedans))
                {
                    totalGas = 270;
                    maxEngOilHealth = 630;
                    maxTransOilHealth = 720;
                }
                else if (v.ClassType.Equals(VehicleClass.Muscle))
                {
                    totalGas = 290;
                    maxEngOilHealth = 690;
                    maxTransOilHealth = 600;
                }
                else if (v.ClassType.Equals(VehicleClass.SUVs))
                {
                    totalGas = 330;
                    maxEngOilHealth = 710;
                    maxTransOilHealth = 760;
                }
                else if (v.ClassType.Equals(VehicleClass.Utility))
                {
                    totalGas = 340;
                    maxEngOilHealth = 750;
                    maxTransOilHealth = 860;
                }
                else if (v.ClassType.Equals(VehicleClass.Vans))
                {
                    totalGas = 220;
                    maxEngOilHealth = 650;
                    maxTransOilHealth = 760;
                }
                else if (v.ClassType.Equals(VehicleClass.Service))
                {
                    totalGas = 200;
                    maxEngOilHealth = 550;
                    maxTransOilHealth = 560;
                }
                else if (v.ClassType.Equals(VehicleClass.OffRoad))
                {
                    totalGas = 270;
                    maxEngOilHealth = 450;
                    maxTransOilHealth = 460;
                }
                else if (v.ClassType.Equals(VehicleClass.Industrial))
                {
                    totalGas = 350;
                    maxEngOilHealth = 760;
                    maxTransOilHealth = 850;
                }
                else if (v.ClassType.Equals(VehicleClass.Emergency))
                {
                    totalGas = 310;
                    maxEngOilHealth = 660;
                    maxTransOilHealth = 770;
                }
                else if (v.ClassType.Equals(VehicleClass.Compacts))
                {
                    totalGas = 130;
                    maxEngOilHealth = 450;
                    maxTransOilHealth = 460;
                }
                else if (v.ClassType.Equals(VehicleClass.Commercial))
                {
                    totalGas = 180;
                    maxEngOilHealth = 470;
                    maxTransOilHealth = 500;
                }
            }
            else if (v.Model.IsBike || v.Model.IsQuadbike)
            {
                gasPrice = 1.3f;

                totalGas = 90;
                maxEngOilHealth = 120;
                maxTransOilHealth = 150;
            }
            else if (v.Model.IsPlane)
            {
                gasPrice = 97.8f;

                totalGas = 830;
                maxEngOilHealth = 1650;
                maxTransOilHealth = 1760;
            }
            else if (v.Model.IsHelicopter)
            {
                gasPrice = 86.1f;

                totalGas = 620;
                maxEngOilHealth = 1250;
                maxTransOilHealth = 1360;
            }
            else if (v.Model.IsBoat)
            {
                gasPrice = 9.3f;

                totalGas = 100;
                maxEngOilHealth = 250;
                maxTransOilHealth = 260;
            }
        }

        private static bool IsNearStation(List<Blip> l)
        {
            foreach (Blip b in l)
            {
                if (playerPed.IsInRangeOf(b.Position, 10.0f)) return true;
            }

            return false;
        }

        private string currentTime()
        {
            string curTime = "";
            int curHours = Function.Call<int>(Hash.GET_CLOCK_HOURS);
            int curMins = Function.Call<int>(Hash.GET_CLOCK_MINUTES);

            if (curHours < 10) curTime += "0" + curHours;
            else curTime += curHours;

            curTime += " : ";

            if (curMins < 10) curTime += "0" + curMins;
            else curTime += curMins;

            return curTime;
        }
    }
}