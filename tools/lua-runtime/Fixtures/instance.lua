-- 每次执行都返回新的实例状态；不能因脚本路径相同而共享可变 table。
return {
    Message = "中文脚本",
    Count = 0,
    Increase = function(self, amount) self.Count = self.Count + amount end,
    Number = function() return 7 end,
    Empty = function() end,
    Fail = function() error("runtime-probe-failure") end
}
