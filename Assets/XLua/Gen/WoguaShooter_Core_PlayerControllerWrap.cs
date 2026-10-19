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
    public class WoguaShooterCorePlayerControllerWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.Core.PlayerController);
			Utils.BeginObjectRegister(type, L, translator, 0, 10, 5, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ResetSpellState", _m_ResetSpellState);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ActivateMagnet", _m_ActivateMagnet);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ActivateShield", _m_ActivateShield);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ActivateGrowth", _m_ActivateGrowth);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "CanMagnetAttract", _m_CanMagnetAttract);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "TryPreventFatalDamage", _m_TryPreventFatalDamage);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetTutorialInputEnabled", _m_SetTutorialInputEnabled);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetTutorialActionPermissions", _m_SetTutorialActionPermissions);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Jumped", _e_Jumped);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GlideStarted", _e_GlideStarted);
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "IsGliding", _g_get_IsGliding);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "MagnetActive", _g_get_MagnetActive);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "ShieldActive", _g_get_ShieldActive);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "GrowthActive", _g_get_GrowthActive);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "DamageInvincible", _g_get_DamageInvincible);
            
			
			
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
					
					var gen_ret = new WoguaShooter.Core.PlayerController();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.PlayerController constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ResetSpellState(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.ResetSpellState(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ActivateMagnet(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.ActivateMagnet(  );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ActivateShield(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.ActivateShield(  );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ActivateGrowth(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.ActivateGrowth(  );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_CanMagnetAttract(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    UnityEngine.Vector3 _worldPosition;translator.Get(L, 2, out _worldPosition);
                    
                        var gen_ret = gen_to_be_invoked.CanMagnetAttract( _worldPosition );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_TryPreventFatalDamage(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                        var gen_ret = gen_to_be_invoked.TryPreventFatalDamage(  );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetTutorialInputEnabled(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    bool _enabled = LuaAPI.lua_toboolean(L, 2);
                    
                    gen_to_be_invoked.SetTutorialInputEnabled( _enabled );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetTutorialActionPermissions(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    bool _firstJumpEnabled = LuaAPI.lua_toboolean(L, 2);
                    bool _secondJumpEnabled = LuaAPI.lua_toboolean(L, 3);
                    bool _glideEnabled = LuaAPI.lua_toboolean(L, 4);
                    
                    gen_to_be_invoked.SetTutorialActionPermissions( _firstJumpEnabled, _secondJumpEnabled, _glideEnabled );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_IsGliding(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.IsGliding);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_MagnetActive(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.MagnetActive);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_ShieldActive(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.ShieldActive);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_GrowthActive(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.GrowthActive);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_DamageInvincible(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.DamageInvincible);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _e_Jumped(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    int gen_param_count = LuaAPI.lua_gettop(L);
			WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
                System.Action<int> gen_delegate = translator.GetDelegate<System.Action<int>>(L, 3);
                if (gen_delegate == null) {
                    return LuaAPI.luaL_error(L, "#3 need System.Action<int>!");
                }
				
				if (gen_param_count == 3)
				{
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "+")) {
						gen_to_be_invoked.Jumped += gen_delegate;
						return 0;
					} 
					
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "-")) {
						gen_to_be_invoked.Jumped -= gen_delegate;
						return 0;
					} 
					
				}
			} catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
			LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.PlayerController.Jumped!");
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _e_GlideStarted(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    int gen_param_count = LuaAPI.lua_gettop(L);
			WoguaShooter.Core.PlayerController gen_to_be_invoked = (WoguaShooter.Core.PlayerController)translator.FastGetCSObj(L, 1);
                System.Action gen_delegate = translator.GetDelegate<System.Action>(L, 3);
                if (gen_delegate == null) {
                    return LuaAPI.luaL_error(L, "#3 need System.Action!");
                }
				
				if (gen_param_count == 3)
				{
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "+")) {
						gen_to_be_invoked.GlideStarted += gen_delegate;
						return 0;
					} 
					
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "-")) {
						gen_to_be_invoked.GlideStarted -= gen_delegate;
						return 0;
					} 
					
				}
			} catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
			LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.PlayerController.GlideStarted!");
            return 0;
        }
        
		
		
    }
}
