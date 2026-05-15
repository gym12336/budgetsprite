-- ============================================================
-- 记账精灵「记账精灵 Web」演示数据初始化脚本
-- 执行方式：mysql -u root -p budget_sprite < seed.sql
-- 或在 MySQL Workbench / Navicat 中直接运行
-- ============================================================

SET NAMES utf8mb4;
USE budget_sprite;

-- ------------------------------------------------------------
-- 清空旧演示数据（安全：只删 testuser 相关数据）
-- ------------------------------------------------------------
SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM RecordImages  WHERE RecordId IN (SELECT Id FROM Records WHERE UserId IN (SELECT Id FROM Users WHERE Username='testuser'));
DELETE FROM Records       WHERE UserId IN (SELECT Id FROM Users WHERE Username='testuser');
DELETE FROM Budgets       WHERE UserId IN (SELECT Id FROM Users WHERE Username='testuser');
DELETE FROM RecurringRules WHERE UserId IN (SELECT Id FROM Users WHERE Username='testuser');
DELETE FROM FinAccounts   WHERE UserId IN (SELECT Id FROM Users WHERE Username='testuser');
DELETE FROM Categories    WHERE UserId IN (SELECT Id FROM Users WHERE Username='testuser');
DELETE FROM Users         WHERE Username = 'testuser';

-- 清空系统预设分类（重新插入保证 ID 一致）
DELETE FROM Categories WHERE UserId IS NULL;

SET FOREIGN_KEY_CHECKS = 1;

-- ------------------------------------------------------------
-- 用户（密码：Test1234）
-- ------------------------------------------------------------
INSERT INTO Users (Id, Username, PasswordHash, Email, Nickname, AvatarUrl, CreatedAt)
VALUES (1, 'testuser',
  '$2a$11$eIGM8O7TGUZdHqd5IwF9PuFgO7DyxwupwC82HM/y.bIvb.XYzJ52S',
  'test@test.com', 'testuser', NULL, '2026-05-08 10:00:00');

-- ------------------------------------------------------------
-- 系统预设分类（UserId=NULL）
-- ------------------------------------------------------------
INSERT INTO Categories (Id, UserId, Name, ParentId, Icon, Color, Type, SortOrder) VALUES
(8,  NULL, '餐饮', NULL, '🍔', '#FF6B6B', 0, 1),
(9,  NULL, '交通', NULL, '🚌', '#4ECDC4', 0, 2),
(10, NULL, '购物', NULL, '🛍️', '#45B7D1', 0, 3),
(11, NULL, '娱乐', NULL, '🎮', '#96CEB4', 0, 4),
(12, NULL, '医疗', NULL, '💊', '#FFEAA7', 0, 5),
(13, NULL, '工资', NULL, '💰', '#6C5CE7', 1, 1),
(14, NULL, '副业', NULL, '💼', '#A29BFE', 1, 2);

-- ------------------------------------------------------------
-- 资金账户
-- ------------------------------------------------------------
INSERT INTO FinAccounts (Id, UserId, Name, Type, Balance, Note) VALUES
(4, 1, '微信钱包',   4, 4965.00, '日常消费'),
(5, 1, '建行储蓄卡', 1, 12000.00, '工资卡'),
(6, 1, '现金',       0, 500.00,  '零花钱');

-- ------------------------------------------------------------
-- 预算（2026-05）
-- ------------------------------------------------------------
INSERT INTO Budgets (Id, UserId, CategoryId, YearMonth, Amount) VALUES
(4, 1, NULL, '2026-05', 3000.00),  -- 总预算
(5, 1, 8,    '2026-05',  500.00),  -- 餐饮预算
(6, 1, 10,   '2026-05', 1000.00); -- 购物预算

-- ------------------------------------------------------------
-- 账单记录
-- ------------------------------------------------------------
INSERT INTO Records
  (Id, UserId, AccountId, CategoryId, Amount, Type, OccurredAt, Note, Tags, CreatedAt, UpdatedAt, IsDeleted)
VALUES
-- 4月账单
(26, 1, 5, 13, 8000.00, 1, '2026-04-10 09:00:00', '四月工资',    '工资', NOW(), NOW(), 0),
(27, 1, 4,  8,   55.00, 0, '2026-04-15 12:30:00', '午饭',        '餐饮', NOW(), NOW(), 0),
(28, 1, 4, 11,  120.00, 0, '2026-04-20 19:00:00', '电影+爆米花', '娱乐', NOW(), NOW(), 0),
(25, 1, 4, 10, 1200.00, 0, '2026-04-28 14:00:00', '买鞋',        '购物', NOW(), NOW(), 0),
-- 5月账单
(24, 1, 4, 12,  200.00, 0, '2026-05-02 10:00:00', '看病买药',    '医疗', NOW(), NOW(), 0),
(23, 1, 5, 14,  500.00, 1, '2026-05-03 16:00:00', '兼职收入',    '副业', NOW(), NOW(), 0),
(22, 1, 4,  8,   28.50, 0, '2026-05-05 12:00:00', '外卖',        '午饭', NOW(), NOW(), 0),
(21, 1, 5,  9,  150.00, 0, '2026-05-06 07:30:00', '滴滴打车',    '出行', NOW(), NOW(), 0),
(20, 1, 4, 11,   88.00, 0, '2026-05-07 20:00:00', 'KTV',         '娱乐', NOW(), NOW(), 0),
(19, 1, 4,  8,   62.00, 0, '2026-05-08 19:00:00', '聚餐',        '朋友', NOW(), NOW(), 0),
(18, 1, 4, 10,  299.00, 0, '2026-05-09 15:20:00', '买衣服',      '购物', NOW(), NOW(), 0),
(17, 1, 5, 13, 8000.00, 1, '2026-05-10 09:00:00', '五月工资',    '工资', NOW(), NOW(), 0),
(29, 1, 4,  8,   35.00, 0, '2026-05-14 00:00:00', '吃饭',        '',    NOW(), NOW(), 0),
(16, 1, 4,  9,   12.00, 0, '2026-05-14 08:15:00', '地铁',        '通勤', NOW(), NOW(), 0),
(15, 1, 4,  8,   45.50, 0, '2026-05-14 12:30:00', '午饭',        '餐饮', NOW(), NOW(), 0);

-- ------------------------------------------------------------
-- 重置自增 ID（避免后续插入冲突）
-- ------------------------------------------------------------
ALTER TABLE Users        AUTO_INCREMENT = 10;
ALTER TABLE FinAccounts  AUTO_INCREMENT = 10;
ALTER TABLE Categories   AUTO_INCREMENT = 20;
ALTER TABLE Budgets       AUTO_INCREMENT = 10;
ALTER TABLE Records      AUTO_INCREMENT = 100;

-- ------------------------------------------------------------
-- 验证
-- ------------------------------------------------------------
SELECT '✅ 初始化完成' AS status;
SELECT CONCAT('账单数：', COUNT(*)) AS info FROM Records WHERE UserId=1 AND IsDeleted=0;
SELECT CONCAT('账户数：', COUNT(*)) AS info FROM FinAccounts WHERE UserId=1;
SELECT CONCAT('预算数：', COUNT(*)) AS info FROM Budgets WHERE UserId=1;
SELECT CONCAT('分类数：', COUNT(*)) AS info FROM Categories;
