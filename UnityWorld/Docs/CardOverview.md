# 卡牌速览

## FormBase

- **剑斩|card_form_jian_zhan**｜基础剑招，造成3点<武器>斩伤｜Size1 CD5 ZhaoShi｜灵耗:无
- **剑刺|card_form_jian_ci**｜基础剑招，造成3点<武器>刺伤｜Size1 CD4.5 ZhaoShi｜灵耗:无
- **剑气|card_form_jian_qi**｜基础剑招，造成3点<武器>射伤｜Size1 CD5.5 ZhaoShi｜灵耗:无
- **刀斩|card_form_dao_zhan**｜基础刀招，造成4点<武器>斩伤｜Size1 CD6 ZhaoShi｜灵耗:无
- **刀气|card_form_dao_qi**｜基础刀招，造成4点<武器>射伤｜Size1 CD6.5 ZhaoShi｜灵耗:无
- **刀打|card_form_dao_da**｜基础刀招，造成3点<武器>打伤｜Size1 CD5.5 ZhaoShi｜灵耗:无
- **拳打|card_form_quan_da**｜基础拳招，造成2点<武器>打伤｜Size1 CD4 ZhaoShi｜灵耗:无
- **拳风|card_form_quan_feng**｜基础拳招，造成2点<武器>射伤｜Size1 CD4.5 ZhaoShi｜灵耗:无
- **刺拳|card_form_ci_quan**｜基础拳招，造成2点<武器>刺伤｜Size1 CD3.5 ZhaoShi｜灵耗:无
- **枪刺|card_form_qiang_ci**｜基础枪招，造成4点<武器>刺伤｜Size1 CD6 ZhaoShi｜灵耗:无
- **枪扫|card_form_qiang_sao**｜基础枪招，造成3点<武器>斩伤｜Size1 CD5 ZhaoShi｜灵耗:无
- **枪砸|card_form_qiang_za**｜基础枪招，造成4点<武器>打伤｜Size1 CD7 ZhaoShi｜灵耗:无
- **拳挡|card_fist_block**｜格挡<武器>点｜Size1 CD3 ZhaoShi｜灵耗:无
- **刀挡|card_blade_block**｜格挡<武器>点｜Size1 CD4 ZhaoShi｜灵耗:无

## HuoCardBase

- **射击|card_huo_shot**｜火系射击，造成5点火射伤害｜Size1 CD6 FaShu｜灵耗:Huo1
- **回灵|card_huo_drain**｜立刻抽取3点MP转化为灵元｜Size1 CD5 FaShu｜灵耗:Huo1
- **爆燃|card_huo_burst**｜全卡充能1s｜Size2 CD10 FaShu｜灵耗:Huo2
- **火枪|card_huo_spear**｜火系刺击，造成8点火刺伤害｜Size2 CD15 FaShu｜灵耗:Huo1
- **自燃|card_huo_self_burn**｜给自己加1层燃烧，抽取1灵元｜Size2 CD2 FaShu｜灵耗:无
- **烈焰斩|card_huo_flame_slash**｜4点火斩，击中本体则施加1燃烧｜Size1 CD5 FaShu｜灵耗:Huo1
- **焚烧|card_huo_incinerate**｜转化1点木灵元回蓝1｜Size1 CD4 FaShu｜灵耗:无

## JinCardBase

- **斩击|card_jin_slash**｜金系斩击，造成2点金斩伤害｜Size1 CD2 FaShu｜灵耗:Jin1
- **加速|card_jin_charge**｜充能上方卡牌，使其CD减少1 tick｜Size1 CD5 FaShu｜灵耗:无
- **重斩|card_jin_heavy_slash**｜金系重斩，造成6点金斩伤害｜Size2 CD4 FaShu｜灵耗:Jin1
- **回旋斩|card_jin_whirlwind**｜3点物理斩击，每触发一次攻击卡为自己充能1s｜Size2 CD8 FaShu｜灵耗:无
- **破甲|card_jin_armor_break**｜消除对方5点护盾值｜Size2 CD5 FaShu｜灵耗:无
- **金甲|card_jin_armor**｜展开12点金系护盾｜Size1 CD8 FaShu｜灵耗:Jin2
- **金针|card_jin_needle**｜1点金刺，击中本体则下次manaCost减1｜Size1 CD2 FaShu｜灵耗:Jin1

## MuCardBase

- **刺击|card_mu_stab**｜木系刺击，造成4点木刺伤害｜Size1 CD4 FaShu｜灵耗:Mu1
- **转化|card_mu_convert**｜将至多1个任意灵元转化回蓝条MP｜Size1 CD12 FaShu｜灵耗:无
- **木灵|card_mu_haste**｜加速上方一张卡（CD累计速率提升100%），可叠加｜Size1 CD5 FaShu｜灵耗:Mu1
- **易伤|card_mu_vulnerable**｜给敌方施加1层易伤，下次受到伤害时额外承受伤害｜Size1 CD4 FaShu｜灵耗:Mu1
- **疗愈|card_mu_heal_wound**｜移除己方随机一张Size为1的伤势卡｜Size2 CD15 FaShu｜灵耗:Mu2

## ShuiCardBase

- **回复|card_shui_heal**｜回复2点HP｜Size1 CD3 FaShu｜灵耗:Shui1
- **水盾|card_shui_shield**｜展开水系6点护盾｜Size1 CD5 FaShu｜灵耗:Shui1
- **冻结|card_shui_freeze**｜冻结敌方随机一张正在CD中的卡牌，暂停其CD 1 tick｜Size2 CD3 FaShu｜灵耗:Shui1
- **虚弱|card_shui_weakness**｜给敌方施加1层虚弱，下次出手拼点数值降低｜Size1 CD4 FaShu｜灵耗:Shui1
- **流动|card_shui_displace**｜将敌方最上方的卡牌移到最下方，打乱对手节奏｜Size2 CD10 FaShu｜灵耗:Shui2

## TuCardBase

- **打击|card_tu_strike**｜土系打击，造成3点土打伤害｜Size1 CD3 FaShu｜灵耗:Tu1
- **格挡|card_tu_block**｜土系格挡4点｜Size1 CD3 FaShu｜灵耗:无
- **土缚|card_tu_slow**｜减速敌方随机一张卡（CD累计速率降低50%），可叠加｜Size1 CD5 FaShu｜灵耗:Tu1
- **土盾|card_tu_armor**｜给自身添加1层护甲，受到伤害时先减去护甲值｜Size2 CD5 FaShu｜灵耗:Tu1
- **眩晕|card_tu_stun**｜给敌方施加眩晕0.5秒，眩晕期间敌方所有卡CD停止推进｜Size2 CD8 FaShu｜灵耗:Tu1

## Wound

- **伤口|card_wound_slash**｜斩击造成的伤口，持续自伤｜Size1 CD4 Wound｜灵耗:无
- **流血|card_wound_stab**｜刺击造成的流血，持续自伤｜Size1 CD4 Wound｜灵耗:无
- **骨折|card_wound_crush**｜打击造成的骨折，持续自伤｜Size1 CD4 Wound｜灵耗:无
- **内伤|card_wound_pierce**｜射击造成的内伤，持续自伤｜Size1 CD4 Wound｜灵耗:无
- **烧伤|card_wound_huo**｜火焰灼烧，持续自伤并干扰金灵元｜Size1 CD6 Wound｜灵耗:无
- **冻伤|card_wound_shui**｜寒冰冻伤，持续自伤并干扰火灵元｜Size1 CD6 Wound｜灵耗:无
- **毒伤|card_wound_mu**｜毒素侵蚀，持续自伤并干扰土灵元｜Size1 CD6 Wound｜灵耗:无
- **金伤|card_wound_jin**｜金气割伤，持续自伤并干扰木灵元｜Size1 CD6 Wound｜灵耗:无
- **土伤|card_wound_tu**｜土气淤滞，持续自伤并干扰水灵元｜Size1 CD6 Wound｜灵耗:无
- **重伤|card_wound_severe**｜严重创伤，持续为自己施加易伤｜Size2 CD5 Wound｜灵耗:无
