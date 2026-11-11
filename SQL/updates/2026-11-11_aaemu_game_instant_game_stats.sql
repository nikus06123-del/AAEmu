-- Persistent arena (instant game) rating and match statistics per character.
-- Ratings use a standard Elo system starting at 1000, tracked per battlefield.
CREATE TABLE IF NOT EXISTS `character_instant_game_stats` (
  `character_id` INT NOT NULL COMMENT 'Character Id',
  `battlefield_id` INT NOT NULL COMMENT 'Battlefield (arena) Id',
  `rating` INT NOT NULL DEFAULT '1000' COMMENT 'Elo rating, starts at 1000',
  `wins` INT NOT NULL DEFAULT '0',
  `losses` INT NOT NULL DEFAULT '0',
  `draws` INT NOT NULL DEFAULT '0',
  `kills` INT NOT NULL DEFAULT '0',
  `deaths` INT NOT NULL DEFAULT '0',
  `assists` INT NOT NULL DEFAULT '0',
  `matches` INT NOT NULL DEFAULT '0' COMMENT 'Total matches played',
  `last_match_at` DATETIME NOT NULL COMMENT 'UTC time of the last finished match',
  PRIMARY KEY (`character_id`, `battlefield_id`)
)
COMMENT='Persistent arena (instant game) rating and match statistics per character'
COLLATE='utf8mb4_general_ci'
ENGINE=InnoDB
;
