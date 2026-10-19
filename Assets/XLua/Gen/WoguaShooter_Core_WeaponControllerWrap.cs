#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class WoguaShooterCoreWeaponControllerWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.Core.WeaponController);
			Utils.BeginObjectRegister(type, L, translator, 0, 3, 13, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ActivateWeapon", _m_ActivateWeapon);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ActivateSpell", _m_ActivateSpell);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ResetWeaponState", _m_ResetWeaponState);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "CurrentType", _g_get_CurrentType);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "SecondaryType", _g_get_SecondaryType);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "PoweredTime", _g_get_PoweredTime);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "SecondaryPoweredTime", _g_get_SecondaryPoweredTime);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "HasSecondMuzzle", _g_get_HasSecondMuzzle);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "DualMuzzleActive", _g_get_DualMuzzleActive);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "DualMuzzleTime", _g_get_DualMuzzleTime);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "TripleShotActive", _g_get_TripleShotActive);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "RepeaterActive", _g_get_RepeaterActive);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "TripleShotTime", _g_get_TripleShotTime);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "RepeaterTime", _g_get_RepeaterTime);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "WaveFlightActive", _g_get_WaveFlightActive);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "WaveFlightTime", _g_get_WaveFlightTime);
            
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 0, 0);
			
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new WoguaShooter.Core.WeaponController();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.WeaponController constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ActivateWeapon(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    WoguaShooter.Config.WeaponType _type;translator.Get(L, 2, out _type);
                    
                    gen_to_be_invoked.ActivateWeapon( _type );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ActivateSpell(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    WoguaShooter.Config.SpellType _type;translator.Get(L, 2, out _type);
                    
                    gen_to_be_invoked.ActivateSpell( _type );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ResetWeaponState(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.ResetWeaponState(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_CurrentType(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.CurrentType);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_SecondaryType(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.SecondaryType);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_PoweredTime(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.PoweredTime);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_SecondaryPoweredTime(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.SecondaryPoweredTime);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_HasSecondMuzzle(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.HasSecondMuzzle);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_DualMuzzleActive(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.DualMuzzleActive);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_DualMuzzleTime(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.DualMuzzleTime);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_TripleShotActive(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.TripleShotActive);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_RepeaterActive(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.RepeaterActive);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_TripleShotTime(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.TripleShotTime);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_RepeaterTime(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.RepeaterTime);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_WaveFlightActive(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.WaveFlightActive);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_WaveFlightTime(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.WeaponController gen_to_be_invoked = (WoguaShooter.Core.WeaponController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushnumber(L, gen_to_be_invoked.WaveFlightTime);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
		
		
    }
}
