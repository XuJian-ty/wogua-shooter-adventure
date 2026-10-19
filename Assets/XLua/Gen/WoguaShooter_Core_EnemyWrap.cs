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
    public class WoguaShooterCoreEnemyWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.Core.Enemy);
			Utils.BeginObjectRegister(type, L, translator, 0, 4, 5, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Kill", _m_Kill);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "KillByGrowth", _m_KillByGrowth);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "MarkTutorialTarget", _m_MarkTutorialTarget);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "TryStomp", _m_TryStomp);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "IsAlive", _g_get_IsAlive);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "IsFloating", _g_get_IsFloating);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "IsTutorialTarget", _g_get_IsTutorialTarget);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "CanBeHitByProjectile", _g_get_CanBeHitByProjectile);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "IsOnScreen", _g_get_IsOnScreen);
            
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 2, 0, 0);
			
			Utils.RegisterFunc(L, Utils.CLS_IDX, "Defeated", _e_Defeated);
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new WoguaShooter.Core.Enemy();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.Enemy constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Kill(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.Kill(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_KillByGrowth(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    UnityEngine.Vector3 _feedbackPosition;translator.Get(L, 2, out _feedbackPosition);
                    
                    gen_to_be_invoked.KillByGrowth( _feedbackPosition );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_MarkTutorialTarget(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    bool _requiresStomp = LuaAPI.lua_toboolean(L, 2);
                    
                    gen_to_be_invoked.MarkTutorialTarget( _requiresStomp );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_TryStomp(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    UnityEngine.Collider2D _contactedCollider = (UnityEngine.Collider2D)translator.GetObject(L, 2, typeof(UnityEngine.Collider2D));
                    WoguaShooter.Core.PlayerController _player = (WoguaShooter.Core.PlayerController)translator.GetObject(L, 3, typeof(WoguaShooter.Core.PlayerController));
                    
                        var gen_ret = gen_to_be_invoked.TryStomp( _contactedCollider, _player );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_IsAlive(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.IsAlive);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_IsFloating(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.IsFloating);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_IsTutorialTarget(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.IsTutorialTarget);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_CanBeHitByProjectile(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.CanBeHitByProjectile);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_IsOnScreen(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.Enemy gen_to_be_invoked = (WoguaShooter.Core.Enemy)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.IsOnScreen);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
		
		
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _e_Defeated(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    int gen_param_count = LuaAPI.lua_gettop(L);
                System.Action<WoguaShooter.Core.Enemy, bool> gen_delegate = translator.GetDelegate<System.Action<WoguaShooter.Core.Enemy, bool>>(L, 2);
                if (gen_delegate == null) {
                    return LuaAPI.luaL_error(L, "#2 need System.Action<WoguaShooter.Core.Enemy, bool>!");
                }
                
				
				if (gen_param_count == 2 && LuaAPI.xlua_is_eq_str(L, 1, "+")) {
					WoguaShooter.Core.Enemy.Defeated += gen_delegate;
					return 0;
				} 
				
				
				if (gen_param_count == 2 && LuaAPI.xlua_is_eq_str(L, 1, "-")) {
					WoguaShooter.Core.Enemy.Defeated -= gen_delegate;
					return 0;
				} 
				
			} catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
			return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.Enemy.Defeated!");
        }
        
    }
}
