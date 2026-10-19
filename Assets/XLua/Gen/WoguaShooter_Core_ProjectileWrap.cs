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
    public class WoguaShooterCoreProjectileWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.Core.Projectile);
			Utils.BeginObjectRegister(type, L, translator, 0, 1, 0, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Launch", _m_Launch);
			
			
			
			
			
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
					
					var gen_ret = new WoguaShooter.Core.Projectile();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.Projectile constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Launch(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.Projectile gen_to_be_invoked = (WoguaShooter.Core.Projectile)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& translator.Assignable<WoguaShooter.Config.WeaponType>(L, 2)&& translator.Assignable<WoguaShooter.Config.WeaponConfig>(L, 3)) 
                {
                    WoguaShooter.Config.WeaponType _type;translator.Get(L, 2, out _type);
                    WoguaShooter.Config.WeaponConfig _config = (WoguaShooter.Config.WeaponConfig)translator.GetObject(L, 3, typeof(WoguaShooter.Config.WeaponConfig));
                    
                    gen_to_be_invoked.Launch( _type, _config );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 4&& translator.Assignable<WoguaShooter.Config.WeaponType>(L, 2)&& translator.Assignable<WoguaShooter.Config.WeaponConfig>(L, 3)&& translator.Assignable<UnityEngine.Vector2>(L, 4)) 
                {
                    WoguaShooter.Config.WeaponType _type;translator.Get(L, 2, out _type);
                    WoguaShooter.Config.WeaponConfig _config = (WoguaShooter.Config.WeaponConfig)translator.GetObject(L, 3, typeof(WoguaShooter.Config.WeaponConfig));
                    UnityEngine.Vector2 _launchDirection;translator.Get(L, 4, out _launchDirection);
                    
                    gen_to_be_invoked.Launch( _type, _config, _launchDirection );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 7&& translator.Assignable<WoguaShooter.Config.WeaponType>(L, 2)&& translator.Assignable<WoguaShooter.Config.WeaponConfig>(L, 3)&& translator.Assignable<UnityEngine.Vector2>(L, 4)&& LuaTypes.LUA_TBOOLEAN == LuaAPI.lua_type(L, 5)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 6)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 7)) 
                {
                    WoguaShooter.Config.WeaponType _type;translator.Get(L, 2, out _type);
                    WoguaShooter.Config.WeaponConfig _config = (WoguaShooter.Config.WeaponConfig)translator.GetObject(L, 3, typeof(WoguaShooter.Config.WeaponConfig));
                    UnityEngine.Vector2 _launchDirection;translator.Get(L, 4, out _launchDirection);
                    bool _useWaveTrajectory = LuaAPI.lua_toboolean(L, 5);
                    float _trajectoryAmplitude = (float)LuaAPI.lua_tonumber(L, 6);
                    float _trajectoryFrequency = (float)LuaAPI.lua_tonumber(L, 7);
                    
                    gen_to_be_invoked.Launch( _type, _config, _launchDirection, _useWaveTrajectory, _trajectoryAmplitude, _trajectoryFrequency );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.Projectile.Launch!");
            
        }
        
        
        
        
        
        
		
		
		
		
    }
}
