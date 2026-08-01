using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plana.Roles
{
    public class CanSabotageAbility : AbstractPlayerAbility,IPlayerAbility
    {
        public CanSabotageAbility(GamePlayer player) : base(player)
        {
        }
        bool IPlayerAbility.CanInvokeSabotage => true;
    }
    public class BlockSabotageAbility : AbstractPlayerAbility, IPlayerAbility
    {
        public BlockSabotageAbility(GamePlayer player) : base(player)
        {
        }
        bool IPlayerAbility.BlockSabotage => true;
    }
}
