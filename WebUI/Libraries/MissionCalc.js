var g_missioncalcs = {};

function MissionCalc(b) {
    $WH.cO(this, b);
    this.DEBUG = $WH.isset("g_dev") && g_dev;
    this.Hash = new HashClass(this);
    this.skipHash = 0;
    this.minItemLevel = {
        "default": 600,
        "5": 630
    };
    this.maxItemLevel = 675;
    this.maxLevel = 100;
    if (this.id) {
        var a = this.id;
        if (this.parent) {
            var c = $WH.ce("div");
            c.id = a;
            $WH.ae($WH.ge(this.parent), c);
            this.container = c
        } else {
            this.container = $WH.ge(a)
        }
    } else {
        return
    }
    if (!this.mission) {
        return
    }
    if (!this.qualityConstant) {
        this.qualityConstant = {
            minimum: 2,
            maximum: 4
        }
    }
    this.chanceAnimObj = {};
    g_missioncalcs[this.id] = this;
    this.Initialize()
}
MissionCalc.prototype = {
    Initialize: function() {
        this.div = $WH.ce("div");
        this.div.className = "mission-calc";
        $WH.ae(this.container, this.div);
        this.controlsDiv = $WH.ce("div");
        this.controlsDiv.className = "mission-calc-controls";
        $WH.ae(this.div, this.controlsDiv);
        this.successDiv = $WH.ce("div");
        this.successDiv.className = "mission-calc-success";
        $WH.ae(this.div, this.successDiv);
        this.sideDiv = $WH.ce("div");
        this.sideDiv.className = "mission-calc-sides";
        this.side = "alliance";
        this.sideDiv.setAttribute("data-side", this.side);
        var h = ["alliance", "horde"];
        for (var b = 0, e; e = h[b]; b++) {
            var d = $WH.ce("a");
            d.className = "mission-calc-side side-" + e + " icon-" + e;
            $WH.ae(this.sideDiv, d);
            $WH.ae(d, document.createTextNode(g_sides[b + 1]));
            $WH.aE(d, "click", this.SetSide.bind(this, e))
        }
        $WH.ae(this.controlsDiv, this.sideDiv);
        this.profiler = false;
        this.profilerMenu = [
            [0, LANG.som.all, this.SetProfile.bind(this, 0)]
        ];
        this.currentProfile = 0;
        if (g_user && g_user.lists && g_user.lists.length) {
            for (var b = 0; b < g_user.lists.length; b++) {
                for (var a = 0; a < g_user.lists[b].lists.length; a++) {
                    if (g_user.lists[b].lists[a].type == 20) {
                        if (!this.profiler) {
                            this.profiler = {}
                        }
                        this.profiler[g_user.lists[b].id] = [];
                        $.ajax({
                            context: this.profiler,
                            beforeSend: function(j, i) {
                                j._WH_list = g_user.lists[b].id
                            },
                            success: function(m, j, l) {
                                for (var i = 0; i < m.length; i++) {
                                    this[l._WH_list][m[i][0]] = {
                                        follower: m[i][0],
                                        active: m[i][1],
                                        quality: m[i][2],
                                        level: m[i][3],
                                        avgilvl: m[i][4],
                                        weaponilvl: m[i][5],
                                        armorilvl: m[i][6],
                                        abilities: m[i][7]
                                    }
                                }
                            },
                            url: "/list=" + g_user.lists[b].id + "&tab=" + g_user.lists[b].lists[a].id + "&json&c=" + g_user.lists[b].updated.replace(/[^\d]/g, "")
                        });
                        this.profilerMenu.push([g_user.lists[b].id, g_user.lists[b].name, this.SetProfile.bind(this, g_user.lists[b].id)])
                    }
                }
            }
        }
        var k = $WH.ce("a");
        k.className = "fa fa-users";
        $WH.ae(this.controlsDiv, k);
        $WH.ae(k, document.createTextNode(LANG.missioncalc_myfollowers));
        if (this.profiler) {
            k.menu = this.profilerMenu;
            Menu.add(k, k.menu, {
                showAtElement: true
            });
            (Menu.findItem(this.profilerMenu, [0])).checked = true
        } else {
            var c = function() {
                var j = 0;
                for (var i in this.followers) {
                    if (this.followers[i].hasOwnProperty("follower") && this.followers[i].follower.id) {
                        j++
                    }
                }
                if (j > 0) {
                    if (!confirm(LANG.missioncalc_leavecheck)) {
                        return
                    }
                }
                location.href = "/client"
            };
            $WH.aE(k, "click", c.bind(this));
            $WH.Tooltip.simple(k, LANG.missioncalc_clientprofilertip)
        }
        this.successChanceDiv = $WH.ce("div");
        this.successChanceDiv.className = "mission-calc-success-chance q0";
        this.successSpan = $WH.ce("span");
        $WH.ae(this.successChanceDiv, this.successSpan);
        $WH.ae(this.successChanceDiv, document.createTextNode(" " + LANG.missioncalc_successchance));
        $WH.ae(this.successDiv, this.successChanceDiv);
        this.maxLevelDiv = $WH.ce("div");
        this.maxLevelDiv.className = "mission-calc-control maxlevel";
        $WH.ae(this.controlsDiv, this.maxLevelDiv);
        this.forceMaxlevelInput = $WH.ce("input");
        this.forceMaxlevelInput.type = "checkbox";
        this.forceMaxlevelInput.id = "maxlevel";
        $WH.ae(this.maxLevelDiv, this.forceMaxlevelInput);
        this.maxLevelLabel = $WH.ce("label");
        this.maxLevelLabel.htmlFor = "maxlevel";
        $WH.ae(this.maxLevelLabel, $WH.ct(LANG.missioncalc_optionmaxlevel));
        $WH.ae(this.maxLevelDiv, this.maxLevelLabel);
        $WH.aE(this.forceMaxlevelInput, "click", this.OnChangeMaxLevelOption.bind(this));
        this.fastestDiv = $WH.ce("div");
        this.fastestDiv.className = "mission-calc-control fastest";
        $WH.ae(this.controlsDiv, this.fastestDiv);
        this.fastestInput = $WH.ce("input");
        this.fastestInput.type = "checkbox";
        this.fastestInput.id = "fastest";
        $WH.ae(this.fastestDiv, this.fastestInput);
        this.fastestLabel = $WH.ce("label");
        this.fastestLabel.htmlFor = "fastest";
        $WH.ae(this.fastestLabel, $WH.ct(LANG.missioncalc_fastest));
        $WH.Tooltip.simple(this.fastestLabel, LANG.missioncalc_priofastesttip);
        $WH.ae(this.fastestDiv, this.fastestLabel);
        $WH.aE(this.fastestInput, "click", this.OnChangeFastestOption.bind(this));
        var g = $WH.ce("a");
        g.className = "fa fa-random";
        $WH.ae(this.controlsDiv, g);
        $WH.ae(g, document.createTextNode(LANG.missioncalc_autofill));
        $WH.Tooltip.simple(g, LANG.missioncalc_autofilltip);
        $WH.aE(g, "click", (function(i) {
            return function() {
                if (i.profiler) {
                    if (!i.currentProfile) {
                        i.SetProfile.call(i, i.profilerMenu[1][0])
                    }
                    i.AutoFill.call(i)
                } else {
                    return alert(LANG.missioncalc_selectprofilefirsttip)
                }
            }
        })(this));
        this.suggestionsDiv = $WH.ce("div");
        this.suggestionsDiv.className = "mission-calc-suggestions q0";
        this.suggestionsDiv.style.display = "none";
        this.followers = {};
        for (var f = 1; f <= this.mission.followers; f++) {
            this.CreateFollower(f);
            $WH.ae(this.div, this.followers[f].div)
        }
        $WH.ae(this.successDiv, this.suggestionsDiv);
        this.UpdateSuccessChance();
        window.setTimeout((function() {
            var i = this.Hash.Read();
            $WH.aE(window, "hashchange", this.Hash.Read);
            if (i) {
                this.Hash.Create()
            }
        }).bind(this), 250);
        if (this.callback != null) {
            this.callback()
        }
    },
    SetSide: function(b) {
        this.skipHash++;
        this.side = b;
        this.sideDiv.setAttribute("data-side", this.side);
        for (var a in this.followers) {
            if (this.followers[a].follower.hasOwnProperty("id")) {
                this.UpdateFollowerDisplay(a)
            }
        }
        if (--this.skipHash == 0) {
            this.Hash.Create()
        }
    },
    SetProfile: function(c) {
        (Menu.findItem(this.profilerMenu, [this.currentProfile])).checked = false;
        this.currentProfile = c;
        (Menu.findItem(this.profilerMenu, [this.currentProfile])).checked = true;
        for (var a = 0, b; b = g_user.lists[a]; a++) {
            if (b.id != c) {
                continue
            }
            if (this.side != g_file_factions[g_file_racefactions[b.race]]) {
                this.SetSide(g_file_factions[g_file_racefactions[b.race]])
            }
            break
        }
    },
    CreateFollower: function(a) {
        var b;
        if (!this.followers.hasOwnProperty(a)) {
            b = {
                follower: {}
            };
            b.div = $WH.ce("div")
        } else {
            b = this.followers[a];
            $WH.ee(b.div)
        }
        b.div.className = "follower follower" + a + " empty";
        b.nameDiv = $WH.ce("div");
        b.nameDiv.className = "name";
        $WH.ae(b.div, b.nameDiv);
        $WH.st(b.nameDiv, LANG.missioncalc_chooseafollower);
        $WH.aE(b.nameDiv, "click", this.ChooseFollower.bind(this, a));
        b.avatarDiv = $WH.ce("div");
        b.avatarDiv.className = "no-empty avatar";
        $WH.ae(b.div, b.avatarDiv);
        $WH.aE(b.avatarDiv, "click", this.ChooseFollower.bind(this, a));
        $WH.Tooltip.simple(b.avatarDiv, LANG.missioncalc_clicktochange, "q2");
        b.avatarPortrait = $WH.ce("div");
        b.avatarPortrait.className = "no-empty avatar-portrait";
        $WH.ae(b.avatarDiv, b.avatarPortrait);
        b.avatarLevel = $WH.ce("div");
        b.avatarLevel.className = "no-empty avatar-level";
        b.avatarLevel.innerHTML = "";
        $WH.ae(b.avatarDiv, b.avatarLevel);
        b.abilitiesDiv = $WH.ce("div");
        b.abilitiesDiv.className = "no-empty abilities";
        $WH.ae(b.div, b.abilitiesDiv);
        b.controlsDiv = $WH.ce("div");
        b.controlsDiv.className = "no-empty controls";
        $WH.ae(b.div, b.controlsDiv);
        b.qualitySelect = $WH.ce("div");
        b.qualitySelect.className = "quality";
        $WH.ae(b.controlsDiv, b.qualitySelect);
        b.levelSelect = $WH.ce("div");
        b.levelSelect.className = "follower-level";
        b.levelSelect.onmousedown = $WH.rf;
        b.levelSelect.onclick = function(d) {
            d.stopPropagation();
            return false
        };
        $WH.ae(b.div, b.levelSelect);
        b.itemLevelSelect = $WH.ce("div");
        b.itemLevelSelect.className = "follower-itemlevel";
        b.itemLevelSelect.onclick = function(d) {
            d.stopPropagation();
            return false
        };
        $WH.ae(b.div, b.itemLevelSelect);
        var c = this.minItemLevel[b.follower.quality] || this.minItemLevel["default"];
        b.itemLevelSlider = Slider.init(b.itemLevelSelect, {
            minValue: c,
            maxValue: this.maxItemLevel,
            trackSize: 154,
            handleSize: 9,
            onMove: this.UpdateFollowerItemLevel.bind(this, a)
        });
        Slider.setValue(b.itemLevelSlider, this.forceMaxlevelInput.checked ? this.maxItemLevel : (b.chosenItemLevel ? b.chosenItemLevel : 600));
        b.itemLevelSlider.onmouseover = function(d) {
            $WH.Tooltip.showAtCursor(d, LANG.tooltip_changeitemlevel2, 0, 0, "q2")
        };
        b.itemLevelSlider.onmousemove = $WH.Tooltip.cursorUpdate;
        b.itemLevelSlider.onmouseout = $WH.Tooltip.hide;
        b.itemLevelSlider.input.onmouseover = function(d) {
            $WH.Tooltip.showAtCursor(d, LANG.tooltip_changeitemlevel, 0, 0, "q2")
        };
        b.itemLevelSlider.input.onmousemove = $WH.Tooltip.cursorUpdate;
        b.itemLevelSlider.input.onmouseout = $WH.Tooltip.hide;
        this.followers[a] = b;
        if (!this.updatingDisplay) {
            this.UpdateFollowerChosenAttributes(a)
        }
    },
    CheckToDisableItemLevel: function(a) {
        var b = this.followers[a];
        if (b.levelSelect) {
            if (!this.forceMaxlevelInput.checked && b.chosenLevel != this.maxLevel) {
                b.itemLevelSelect.style.display = "none"
            } else {
                b.itemLevelSelect.style.display = "block"
            }
            this.UpdateFollowerItemLevel(a)
        }
    },
    UpdateFollowerChosenAttributes: function(a) {
        this.skipHash++;
        this.UpdateFollowerQuality(a);
        this.UpdateFollowerLevel(a);
        this.UpdateFollowerItemLevel(a);
        if (--this.skipHash == 0) {
            this.Hash.Create()
        }
    },
    UpdateFollowerQuality: function(b, a) {
        var c = this.followers[b];
        c.chosenQuality = a || c.div.getAttribute("data-quality");
        this.UpdateFollowerDisplay(b);
        this.UpdateSuccessChance()
    },
    UpdateFollowerLevel: function(b, a) {
        var c = this.followers[b];
        if (a < this.maxLevel) {
            this.forceMaxlevelInput.checked = false
        }
        c.chosenLevel = a || c.div.getAttribute("data-level");
        this.UpdateFollowerDisplay(b);
        this.CheckToDisableItemLevel(b);
        this.UpdateSuccessChance()
    },
    UpdateFollowerItemLevel: function(c, d, b, a) {
        this.skipHash++;
        var e = this.followers[c];
        if (a && a.value < this.maxItemLevel) {
            this.forceMaxlevelInput.checked = false
        }
        if (!e.chosenItemLevel) {
            if (e.itemLevelSlider && e.itemLevelSlider._min) {
                e.chosenItemLevel = e.itemLevelSlider._min
            } else {
                e.chosenItemLevel = 600
            }
        }
        if (a && a.value) {
            e.chosenItemLevel = a.value
        }
        this.UpdateSuccessChance();
        if (--this.skipHash == 0) {
            this.Hash.Create()
        }
    },
    ChooseFollower: function(a) {
        this.choosingSlot = a;
        Lightbox.show("follower", {
            onShow: this.picker.onFollowerPickerShow.bind(this)
        })
    },
    UpdateFollowerDisplay: function(c) {
        if (this.updatingDisplay) {
            return
        }
        this.updatingDisplay = true;
        this.skipHash++;
        var j = this.followers[c];
        this.CreateFollower(c);
        if (!j.follower.hasOwnProperty("id")) {
            this.skipHash--;
            this.updatingDisplay = false;
            return
        }
        j.div.setAttribute("data-class", j.follower[this.side].baseclass);
        j.avatarPortrait.style.backgroundImage = 'url("' + g_staticUrl + "/images/wow/garr/" + j.follower[this.side].portrait + '.png")';
        var l = $WH.ce("a");
        l.href = "/follower=" + j.follower.id + "." + (this.side == "alliance" ? "1" : "2");
        l.rel = [j.chosenQuality ? "q=" + j.chosenQuality : false, j.chosenLevel ? "level=" + j.chosenLevel : false, (j.chosenAbilities && j.chosenAbilities.length) ? "abil=" + j.chosenAbilities.join(":") : false].filter(function(f) {
            return !!f
        }).join("&");
        l.className = "q";
        $WH.st(l, j.follower[this.side].name);
        $WH.aE(l, "click", function(a) {
            if (a.button == 0) {
                a.preventDefault()
            }
        });
        $WH.ee(j.nameDiv);
        $WH.ae(j.nameDiv, l);
        $WH.Tooltip.simple(j.nameDiv, LANG.missioncalc_clicktochange, "q2");
        $WH.ee(j.abilitiesDiv);
        var g = function(q, r) {
            if (!g_garrison_abilities.hasOwnProperty(q)) {
                return
            }
            var p = g_garrison_abilities[q];
            var a = Icon.create(p.icon, 0, null, "javascript:;");
            a.className += " garrison-ability";
            var f = a.getElementsByTagName("a")[0];
            f.rel = "garrisonability=" + p.id;
            if (r >= 0) {
                $WH.aE(a, "click", this.ChooseAbility.bind(this, c, r));
                $WH.Tooltip.addTooltipText(f, LANG.missioncalc_clicktochange, "q2")
            }
            $WH.ae(j.abilitiesDiv, a)
        };
        var d = this.AllowedAbilityAddition(c, -1);
        if (j.chosenAbilities) {
            for (x in j.chosenAbilities) {
                g.call(this, j.chosenAbilities[x], x)
            }
        }
        if (d) {
            var e = $WH.ce("a");
            e.href = "javascript:;";
            e.className = "fa fa-plus";
            $WH.aE(e, "click", this.ChooseAbility.bind(this, c, -1));
            var o = "";
            switch (d) {
                case 1:
                    o = g_garrison_ability_types[0];
                    break;
                case 2:
                    o = g_garrison_ability_types[1];
                    break;
                default:
                    o = g_garrison_ability_types[0] + "/" + g_garrison_ability_types[1];
                    break
            }
            $WH.Tooltip.simple(e, LANG.add + " " + o, "q2", true);
            $WH.ae(j.abilitiesDiv, e)
        }
        $WH.ee(j.qualitySelect);
        if (j.follower.quality < this.qualityConstant.maximum) {
            var b = [];
            for (var n = this.qualityConstant.minimum; n <= this.qualityConstant.maximum; n++) {
                if (n >= j.follower.quality) {
                    b.push([n, g_item_qualities[n], this.UpdateFollowerQuality.bind(this, c, n), null, {
                        className: "q" + n,
                        checkedFunc: function(a) {
                            return a[0] == j.chosenQuality
                        }
                    }])
                }
            }
            j.qualitySelect.menu = b;
            Menu.add(j.qualitySelect, j.qualitySelect.menu)
        }
        if (!j.chosenQuality) {
            j.chosenQuality = j.follower.quality
        }
        j.div.setAttribute("data-quality", j.chosenQuality);
        j.qualitySelect.innerHTML = g_item_qualities[j.chosenQuality];
        j.qualitySelect.className = "quality q" + j.chosenQuality;
        if (j.qualitySelect.menu) {
            j.qualitySelect.className += " fa fa-caret-down fa-placement-spaced"
        }
        $WH.ee(j.levelSelect);
        if (!j.chosenLevel) {
            j.chosenLevel = j.follower.level
        }
        var k = this.forceMaxlevelInput.checked ? this.maxLevel : j.chosenLevel;
        if (j.follower.level < this.maxLevel) {
            var m = [];
            for (var h = 90; h <= this.maxLevel; h++) {
                if (h >= j.follower.level) {
                    m.push([h, h, this.UpdateFollowerLevel.bind(this, c, h), null, {
                        checkedFunc: function(a) {
                            return a[0] == j.chosenLevel
                        }
                    }])
                }
            }
            j.levelSelect.menu = m;
            Menu.add(j.levelSelect, j.levelSelect.menu)
        }
        j.div.setAttribute("data-level", j.chosenLevel);
        j.levelSelect.innerHTML = k;
        j.levelSelect.className = "follower-level level-" + k;
        if (j.levelSelect.menu) {
            j.levelSelect.className += " fa fa-caret-down fa-placement-spaced"
        }
        this.UpdateFollowerChosenAttributes(c);
        j.div.className = j.div.className.replace(" empty", "");
        this.UpdateMechanicCounters();
        this.UpdateSuccessChance();
        if (--this.skipHash == 0) {
            this.Hash.Create()
        }
        this.updatingDisplay = false
    },
    UpdateSuccessChance: function() {
        var a = this;
        $(this.chanceAnimObj).stop();
        this.chanceAnimObj.chance = isNaN(this.chanceAnimObj.chance) ? 0 : this.chanceAnimObj.chance;
        $(this.chanceAnimObj).animate({
            chance: this.CalculateSuccessChance().chance
        }, {
            duration: 250,
            easing: "easeInQuad",
            step: function() {
                $WH.st(a.successSpan, "" + Math.floor(this.chance) + "%")
            },
            complete: function() {
                $WH.st(a.successSpan, "" + this.chance + "%")
            }
        })
    },
    OnChangeMaxLevelOption: function() {
        this.skipHash++;
        for (var a in this.followers) {
            if (this.followers[a].follower.hasOwnProperty("id")) {
                this.UpdateFollowerDisplay(a)
            }
        }
        if (--this.skipHash == 0) {
            this.Hash.Create()
        }
    },
    OnChangeFastestOption: function() {
        this.Hash.Create()
    },
    ChooseAbility: function(c, b) {
        this.choosingSlot = c;
        var a = -1;
        if (b >= 0) {
            a = this.followers[c].chosenAbilities[b]
        }
        this.choosingAbility = {
            index: b,
            allowed: this.AllowedAbilityAddition(c, a)
        };
        Lightbox.show("ability", {
            onShow: this.picker.onAbilityPickerShow.bind(this)
        })
    },
    AllowedAbilityAddition: function(b, a) {
        var e = this.followers[b];
        var h = parseInt(e.chosenQuality || e.follower.quality, 10);
        var c = {
            abilities: 0,
            traits: 0,
            needMore: 0
        };
        var g = function(i) {
            if (!g_garrison_abilities.hasOwnProperty(i)) {
                return
            }
            if (i == a) {
                return
            }
            var f = g_garrison_abilities[i];
            c[f.trait ? "traits" : "abilities"] ++
        };
        if (e.chosenAbilities) {
            for (x in e.chosenAbilities) {
                g(e.chosenAbilities[x])
            }
        }
        var d = function(j, i) {
            if (i <= 0) {
                return
            }
            if ((!e.chosenAbilities) || (!e.chosenAbilities.length)) {
                return
            }
            for (var f = 0; f < e.chosenAbilities.length; f++) {
                var k = e.chosenAbilities[f];
                if (g_garrison_abilities[k].trait == ((j & 2) > 0)) {
                    e.chosenAbilities.splice(f--, 1);
                    c[g_garrison_abilities[k].trait ? "traits" : "abilities"] --;
                    if (--i <= 0) {
                        break
                    }
                }
            }
        };
        switch (h) {
            case 2:
                d(1, c.abilities - 1);
                d(2, c.traits - 1);
                c.needMore += (c.abilities < 1 ? 1 : 0) + (c.traits < 1 ? 2 : 0);
                break;
            case 3:
                d(1, c.abilities - 2);
                d(2, c.traits - 2);
                d(3, c.abilities + c.traits - 3);
                if (c.abilities + c.traits >= 3) {
                    c.needMore = 0
                } else {
                    if (c.abilities >= 2) {
                        c.needMore = 2
                    } else {
                        if (c.traits >= 2) {
                            c.needMore = 1
                        } else {
                            c.needMore = 3
                        }
                    }
                }
                break;
            case 4:
                d(1, c.abilities - 2);
                d(2, c.traits - 3);
                c.needMore += (c.abilities < 2 ? 1 : 0) + (c.traits < 3 ? 2 : 0);
                break;
            case 5:
                d(1, c.abilities - 3);
                d(2, c.traits - 3);
                c.needMore += (c.abilities < 3 ? 1 : 0) + (c.traits < 3 ? 2 : 0);
                break
        }
        return c.needMore
    },
    UpdateMechanicCounters: function() {
        var e = {};
        var c, b;
        for (var a = 1; a <= this.mission.followers; a++) {
            if (this.followers.hasOwnProperty(a) && this.followers[a].follower.hasOwnProperty("id")) {
                c = this.followers[a].follower;
                if (this.followers[a].chosenAbilities) {
                    for (var g = 0; g < this.followers[a].chosenAbilities.length; g++) {
                        if (g_garrison_abilities.hasOwnProperty(this.followers[a].chosenAbilities[g])) {
                            b = g_garrison_abilities[this.followers[a].chosenAbilities[g]];
                            for (var d = 0; d < b.counters.length; d++) {
                                if (b.counters[d]) {
                                    if (!e.hasOwnProperty(b.counters[d])) {
                                        e[b.counters[d]] = 0
                                    }
                                    e[b.counters[d]] ++
                                }
                            }
                        }
                    }
                }
            }
        }
        $(".garrison-encounter-enemy .iconmedium").each(function(h) {
            var i = $(this);
            var f;
            if (i.attr("data-mechanic")) {
                f = i.attr("data-mechanic");
                if (e.hasOwnProperty(f)) {
                    i.addClass("garrison-ability-countered");
                    if (--e[f] <= 0) {
                        delete e[f]
                    }
                } else {
                    i.removeClass("garrison-ability-countered")
                }
            }
        });
        $("table.infobox a").each(function(h) {
            var k = $(this);
            var i = /\/garrisonabilities\?filter=cr=2;crs=(\d+);crv=0$/;
            var j;
            if (j = i.exec(this.href)) {
                var f = j[1];
                k.addClass("mechanic");
                if (e.hasOwnProperty(f)) {
                    k.addClass("countered");
                    if (--e[f] <= 0) {
                        delete e[f]
                    }
                } else {
                    k.removeClass("countered")
                }
            }
        })
    },
    GetFollowerBias: function(e, c) {
        var a = (e - this.mission.level) * $WH.fround(1 / 3);
        if (this.mission.level == this.maxLevel) {
            var d = this.mission.itemlevel;
            if (d <= 0) {
                d = this.minItemLevel["default"]
            }
            if (d > 0) {
                a += (c - d) * $WH.fround(1 / 15)
            }
        }
        var b = -1;
        if (a < -1 || (b = 1, a > 1)) {
            a = b
        }
        return a
    },
    ThreatCounterIsAlreadyRegistered: function(a, b) {
        return this.registeredThreatCounters.hasOwnProperty(a) && this.registeredThreatCounters[a].hasOwnProperty(b) && this.registeredThreatCounters[a][b]
    },
    RegisterThreatCounter: function(a, b, c) {
        if (!this.registeredThreatCounters.hasOwnProperty(a)) {
            this.registeredThreatCounters[a] = {}
        }
        if (!this.registeredThreatCounters[a].hasOwnProperty(b)) {
            this.registeredThreatCounters[a][b] = {}
        }
        this.registeredThreatCounters[a][b][c] = true
    },
    CalcChance: function(b, a, c) {
        var d;
        if (c >= 0) {
            d = (a - b) * c + b
        } else {
            d = (c + 1) * b
        }
        return $WH.fround(d)
    },
    CheckEffectRace: function(c, a, d) {
        if (this.mission.followers > 0) {
            for (var e = 0; e < c.length; ++e) {
                if (e == d) {
                    continue
                }
                var f = c[e];
                var b = g_garrison_followers && g_garrison_followers[f.follower] && g_garrison_followers[f.follower][this.side] ? g_garrison_followers[f.follower][this.side] : null;
                if (b == null) {
                    continue
                }
                if (b.race == a) {
                    return true
                }
            }
        }
        return false
    },
    GetMissionTimes: function(g) {
        var e = this.mission.missiontime;
        var h = this.mission.traveltime;
        for (var c = 0; c < g.length; ++c) {
            var d = g[c];
            for (var b = 0; b < d.abilities.length; ++b) {
                var a = g_garrison_abilities[d.abilities[b]];
                var f = a.type.length;
                for (var k = 0; k < f; ++k) {
                    if (a.type[k] == 3) {
                        h *= a.amount4[k]
                    }
                    if (a.type[k] == 17) {
                        e *= a.amount4[k]
                    }
                }
            }
        }
        return {
            missiontime: Math.floor(e),
            traveltime: Math.floor(h)
        }
    },
    GetMentorInfo: function(h) {
        var f = 0;
        var a = 0;
        if (this.mission.followers > 0) {
            for (var d = 0; d < h.length; ++d) {
                var e = h[d];
                for (var c = 0; c < e.abilities.length; ++c) {
                    var b = g_garrison_abilities[e.abilities[c]];
                    var g = b.type.length;
                    for (var k = 0; k < g; ++k) {
                        if (b.type[k] == 18) {
                            if (e.level > f) {
                                f = e.level
                            }
                            if (e.avgilvl > a) {
                                a = e.avgilvl
                            }
                        }
                    }
                }
            }
        }
        return {
            level: f,
            itemlevel: a
        }
    },
    AutoFill: function() {
        if (!this.profiler[this.currentProfile] || !this.mission.followers) {
            return
        }
        var k = [];
        for (var b in this.profiler[this.currentProfile]) {
            if (this.profiler[this.currentProfile].hasOwnProperty(b) && this.profiler[this.currentProfile][b].active == 1) {
                k.push(parseInt(b))
            }
        }
        if (!k.length) {
            return
        }
        var n = this.GetFollowerCombos(k, this.mission.followers);
        if (this.DEBUG) {
            console.log("Total combinations: %d", n.length)
        }
        var o = [];
        var c;
        var q = this;
        for (var h = 0; h < n.length; ++h) {
            n[h].sort(function(j, i) {
                var t = q.profiler[q.currentProfile][j];
                var s = q.profiler[q.currentProfile][i];
                if (parseInt(t.level) != parseInt(s.level)) {
                    return parseInt(s.level) - parseInt(t.level)
                }
                return parseInt(s.avgilvl) - parseInt(t.avgilvl)
            });
            c = [];
            for (var g = 0; g < n[h].length; ++g) {
                c.push(this.profiler[this.currentProfile][n[h][g]])
            }
            o.push({
                chanceInfo: this.CalculateSuccessChance.call(this, c),
                combo: n[h],
                time: this.GetMissionTimes(c).missiontime
            })
        }
        o.sort(function(j, i) {
            return i.chanceInfo.chance - j.chanceInfo.chance
        });
        var r = [];
        for (h = 0; h < o.length; ++h) {
            if (o[h].chanceInfo.chance >= (o[0].chanceInfo.chance - 5)) {
                r.push(o[h])
            }
        }
        if (r.length > 1) {
            $WH.array_apply(r, function(i) {
                i.xp = q.GetTotalXPGain.call(q, i)
            });
            var l = r[r.length - 1].chanceInfo.chance;
            var m = r[0].chanceInfo.chance;
            var p = -1,
                e = -1,
                f = -1,
                a = -1;
            for (h = 0; h < r.length; ++h) {
                var d = r[h];
                if (p == -1 || p > d.xp) {
                    p = d.xp
                }
                if (e == -1 || e < d.xp) {
                    e = d.xp
                }
                if (f == -1 || f > d.time) {
                    f = d.time
                }
                if (a == -1 || a < d.time) {
                    a = d.time
                }
            }
            r.sort(function(s, j) {
                var t = q.GetComboScore.call(q, s, l, m, p, e, f, a);
                var i = q.GetComboScore.call(q, j, l, m, p, e, f, a);
                return i - t
            });
            if (this.DEBUG) {
                console.log("minChance: %d, maxChance: %d, minXP: %d, maxXP: %d, minTime: %d, maxTime: %d", l, m, p, e, f, a);
                console.log("Eligible combos:");
                for (h = 0; h < r.length; ++h) {
                    console.log("combo %d: %s%, %s XP, %s, %s score", h, r[h].chanceInfo.chance.toFixed(2), r[h].xp.toFixed(2), g_formatTimeElapsed(r[h].time), (this.GetComboScore.call(this, r[h], l, m, p, e, f, a)).toFixed(2))
                }
            }
        }
        this.BuildComboSuggestions.call(this, r);
        this.AutoFillCombo.call(this, 0, r[0])
    },
    BuildComboSuggestions: function(b) {
        $WH.ee(this.suggestionsDiv);
        b = b.slice(0, 3);
        if (b.length > 0) {
            $WH.ae(this.suggestionsDiv, $WH.ct(LANG.missioncalc_suggestions + LANG.colon));
            for (var a = 0; a < b.length; ++a) {
                var c = Math.floor(b[a].xp);
                if (c > 1000) {
                    c = (Math.round(c / 1000 * 10) / 10) + "k"
                }
                $WH.ae(this.suggestionsDiv, $WH.g_createButton(b[a].chanceInfo.chance + "%" + (b[a].xp ? (" (" + c + " XP)") : ""), null, {
                    "float": false,
                    "class": "suggestion",
                    size: "small",
                    click: this.AutoFillCombo.bind(this, a, b[a]),
                    tooltip: this.BuildComboTooltip.call(this, b[a])
                }))
            }
            this.suggestionsDiv.style.display = "inline"
        } else {
            this.suggestionsDiv.style.display = "none"
        }
    },
    BuildComboTooltip: function(e) {
        var a = "",
            b, d;
        var f = [];
        for (var c = 0; c < e.combo.length; ++c) {
            b = this.profiler[this.currentProfile][e.combo[c]];
            f.push(b);
            d = null;
            if ($WH.isset("g_garrison_followers") && g_garrison_followers[b.follower] && g_garrison_followers[b.follower][this.side]) {
                d = g_garrison_followers[b.follower][this.side].name
            }
            if (d && b.quality && b.level && b.avgilvl) {
                a += (a == "" ? "" : "<br>") + '<span class="q' + b.quality + '">[' + (b.level == this.maxLevel ? b.avgilvl : b.level) + "] " + d + "</span>"
            }
        }
        a += "<br>" + g_formatTimeElapsed(this.GetMissionTimes(f).missiontime);
        return a
    },
    AutoFillCombo: function(e, c) {
        var a = $(".mission-calc-suggestions > a");
        a.removeClass("active");
        $(a.get(e)).addClass("active");
        for (var b = 0; b < c.combo.length; ++b) {
            this.choosingSlot = parseInt(b) + 1;
            var d = g_garrison_followers && g_garrison_followers[c.combo[b]] ? g_garrison_followers[c.combo[b]] : null;
            if (d != null) {
                this.picker.chosenFollower.call(this, d)
            }
        }
    },
    GetComboScore: function(i, f, g, j, c, d, b) {
        var e = 0.6;
        var h = 0.4;
        var k = 0;
        if (this.fastestInput.checked) {
            e = 0.2;
            h = 0.2;
            k = 0.6
        }
        return ((f != g) ? (((i.chanceInfo.chance - f) / (g - f)) * e) : 0) + ((j != c) ? (((i.xp - j) / (c - j)) * h) : 0) + ((d != b) ? (((b - i.time) / (b - d)) * k) : 0)
    },
    GetTotalXPGain: function(u) {
        var c = u.combo;
        var q = u.chanceInfo.chanceOver / 100;
        var g = this.mission.experience;
        var a = 0;
        for (var n in this.mission.rewards) {
            if (n == "experience") {
                for (var m = 0; m < this.mission.rewards[n].length; ++m) {
                    a += this.mission.rewards[n][m]
                }
            }
        }
        var e = 0;
        for (n = 0; n < c.length; ++n) {
            var f = this.profiler[this.currentProfile][c[n]];
            var s = this.forceMaxlevelInput.checked ? this.maxLevel : f.level;
            var p = this.forceMaxlevelInput.checked ? this.maxItemLevel : f.avgilvl;
            if (s >= this.maxLevel && f.quality >= 4) {
                continue
            }
            var o = 1;
            if (this.mission.level) {
                if (s <= (this.mission.level - 3)) {
                    o = 0.1
                } else {
                    if (s < this.mission.level) {
                        o = 0.5
                    }
                }
            }
            if (this.mission.itemlevel) {
                if (p <= (this.mission.itemlevel - 11)) {
                    o = 0.1
                } else {
                    if (p < this.mission.itemlevel) {
                        o = 0.5
                    }
                }
            }
            var h = 0;
            for (m = 0; m < c.length; ++m) {
                var d = this.profiler[this.currentProfile][c[m]];
                for (var l = 0; l < d.abilities.length; ++l) {
                    var t = g_garrison_abilities[d.abilities[l]];
                    var b = t.type.length;
                    for (var r = 0; r < b; ++r) {
                        if (t.type[r] == 4) {
                            switch (t.missionparty[r]) {
                                case 2:
                                    h += 1 - t.amount4[r];
                                    break;
                                case 1:
                                    if (n == m) {
                                        h += 1 - t.amount4[r]
                                    }
                                    break;
                                default:
                                    break
                            }
                        }
                    }
                }
            }
            e += (g + (q + h) * g) * o;
            e += (a + a * h) * o
        }
        return e
    },
    GetFollowerCombos: function(a, b) {
        var f, c, e, d, g;
        if (b > a.length || b <= 0) {
            return []
        }
        if (b == a.length) {
            return [a]
        }
        if (b == 1) {
            e = [];
            for (f = 0; f < a.length; f++) {
                e.push([a[f]])
            }
            return e
        }
        e = [];
        for (f = 0; f < a.length - b + 1; f++) {
            d = a.slice(f, f + 1);
            g = this.GetFollowerCombos.call(this, a.slice(f + 1), b - 1);
            for (c = 0; c < g.length; c++) {
                e.push(d.concat(g[c]))
            }
        }
        return e
    },
    CalculateSuccessChance: function(c) {
        var l = (this.DEBUG && !c) ? true : false;
        if (l) {
            console.log("----- Start ComputeSuccessChance -----")
        }
        var r = [];
        if (c) {
            r = c
        } else {
            for (var w in this.followers) {
                if (this.followers[w].hasOwnProperty("follower") && this.followers[w].follower.id) {
                    r.push({
                        follower: this.followers[w].follower.id,
                        abilities: this.followers[w].chosenAbilities ? this.followers[w].chosenAbilities.slice() : [],
                        avgilvl: this.followers[w].chosenItemLevel ? parseInt(this.followers[w].chosenItemLevel) : this.followers[w].follower.itemlevel,
                        level: this.followers[w].chosenLevel ? parseInt(this.followers[w].chosenLevel) : this.followers[w].follower.level,
                        quality: this.followers[w].chosenQuality ? parseInt(this.followers[w].chosenQuality) : this.folì owers[w].follower.quality
                    })
                }
            }
        }
        var d = [];
        if (this.mission && this.mission.encounters) {
            for (w in this.mission.encounters) {
                if (this.mission.encounters[w].mechanics) {
                    for (var v in this.mission.encounters[w].mechanics) {
                        d.push(this.mission.encounters[w].mechanics[v])
                    }
                }
            }
        }
        var n = this.GetMentorInfo(r);
        var q;
        for (w = 0; w < r.length; ++w) {
            q = r[w];
            if (n.level > q.level) {
                if (l) {
                    console.log("Mentored follower %d from level %d to level %d.", q.follower, q.level, n.level)
                }
                q.level = n.level
            }
            if (n.itemlevel > q.avgilvl) {
                if (l) {
                    console.log("Mentored follower %d from item level %d to item level %d.", q.follower, q.avgilvl, n.itemlevel)
                }
                q.avgilvl = n.itemlevel
            }
            var y = q.level;
            var z = q.avgilvl;
            if (this.forceMaxlevelInput.checked) {
                y = this.maxLevel;
                z = this.maxItemLevel
            }
            q.bias = $WH.fround(this.GetFollowerBias(y, z));
            if (l) {
                console.log("Follower %d bias: %.2f", q.follower, q.bias)
            }
        }
        var F = this.mission.followers * 100;
        var D = F;
        if (d.length > 0) {
            for (w = 0; w < d.length; ++w) {
                var E = d[w];
                if (E.category != 2) {
                    F = D
                } else {
                    F = D + E.amount;
                    D += E.amount
                }
            }
        }
        if (F <= 0) {
            return {
                chance: 100,
                chanceOver: 0
            }
        }
        var a = $WH.fround(100 / F);
        if (l) {
            console.log("coeff: ", a)
        }
        var k = 0;
        for (w = 0; w < r.length; ++w) {
            q = r[w];
            var B = $WH.fround(this.CalcChance(100, 150, q.bias) * a);
            k += B;
            if (l) {
                console.log("Added %.2f to success due to follower %d bias.", B, q.follower)
            }
        }
        var C = 0;
        var o = {};
        if (d.length > 0) {
            do {
                E = d[C];
                if (!E.category || E.category == 2) {
                    if (o[E.type]) {
                        o[E.type].amount1 += E.amount;
                        o[E.type].amount2 += E.amount
                    } else {
                        o[E.type] = {
                            amount1: E.amount,
                            amount2: E.amount,
                            id: E.id
                        }
                    }
                }++C
            } while (C < d.length)
        }
        for (var m in o) {
            var b = o[m].amount2;
            if (this.mission.followers > 0) {
                for (w = 0; w < r.length; ++w) {
                    q = r[w];
                    for (v = 0; v < q.abilities.length; ++v) {
                        var I = g_garrison_abilities[q.abilities[v]];
                        var e = I.type.length;
                        for (var H = 0; H < e; ++H) {
                            if (m == I.counters[H] && !(I.amount1[H] & 1) && b > 0) {
                                var G = this.CalcChance(I.amount2[H], I.amount3[H], q.bias);
                                var s = b - G;
                                if (s < 0) {
                                    s = 0
                                }
                                b = s
                            }
                        }
                    }
                }
            }
            v28 = $WH.fround((o[m].amount1 - b) * a);
            k += v28;
            if (l) {
                console.log("Added %.2f to success due to followers countering boss mechanic type %d.", v28, o[m].id)
            }
        }
        for (C = 0; C < d.length; ++C) {
            E = d[C];
            if (E.category == 1) {
                if (this.mission.followers > 0) {
                    for (w = 0; w < r.length; ++w) {
                        q = r[w];
                        for (v = 0; v < q.abilities.length; ++v) {
                            I = g_garrison_abilities[q.abilities[v]];
                            e = I.type.length;
                            for (H = 0; H < e; ++H) {
                                if (E.type == I.counters[H]) {
                                    chance = this.CalcChance(I.amount2[H], I.amount3[H], q.bias);
                                    chance *= a;
                                    chance = $WH.fround(chance);
                                    k += chance;
                                    if (l) {
                                        console.log("Added %.2f to success due to follower %d enemy race ability %d.", chance, q.follower, E.id)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        if (this.mission.followers > 0) {
            for (w = 0; w < r.length; ++w) {
                q = r[w];
                for (v = 0; v < q.abilities.length; ++v) {
                    I = g_garrison_abilities[q.abilities[v]];
                    e = I.type.length;
                    for (H = 0; H < e; ++H) {
                        if (I.counters[H] && I.counters[H] == this.mission.mechanictype) {
                            chance = this.CalcChance(I.amount2[H], I.amount3[H], q.bias);
                            chance *= a;
                            chance = $WH.fround(chance);
                            k += chance;
                            if (l) {
                                console.log("Added %.2f to success due to follower %d environment ability %d.", chance, q.follower, I.id)
                            }
                        }
                    }
                }
            }
        }
        var A = this.GetMissionTimes(r);
        if (this.mission.followers > 0) {
            for (w = 0; w < r.length; ++w) {
                q = r[w];
                for (v = 0; v < q.abilities.length; ++v) {
                    I = g_garrison_abilities[q.abilities[v]];
                    e = I.type.length;
                    for (H = 0; H < e; ++H) {
                        var u = false;
                        switch (I.type[H]) {
                            case 1:
                                if (r.length == 1) {
                                    u = true
                                }
                                break;
                            case 2:
                                u = true;
                                break;
                            case 5:
                                if (this.CheckEffectRace.call(this, r, I.race[H], w)) {
                                    u = true
                                }
                                break;
                            case 6:
                                if (A.missiontime > 3600 * I.hours[H]) {
                                    u = true
                                }
                                break;
                            case 7:
                                if (A.missiontime < 3600 * I.hours[H]) {
                                    u = true
                                }
                                break;
                            case 9:
                                if (A.traveltime > 3600 * I.hours[H]) {
                                    u = true
                                }
                                break;
                            case 10:
                                if (A.traveltime < 3600 * I.hours[H]) {
                                    u = true
                                }
                                break;
                            default:
                                break
                        }
                        if (u) {
                            chance = this.CalcChance(I.amount2[H], I.amount3[H], q.bias);
                            chance *= a;
                            chance = $WH.fround(chance);
                            k += chance;
                            if (l) {
                                console.log("Added %.2f to success due to follower %d trait %d.", chance, q.follower, I.type[H])
                            }
                        }
                    }
                }
            }
        }
        k = $WH.fround(k);
        if (l) {
            console.log("Total before adding base chance: %.2f", k)
        }
        var t = true;
        var h = 100;
        var g;
        var p = (((100 - this.mission.basebonuschance) * k) * 0.01) + this.mission.basebonuschance;
        if (l) {
            console.log("Total after base chance: %.2f", p)
        }
        g = p;
        var f = g;
        if (t && h <= p) {
            g = h
        }
        if (l) {
            if (t && f > 100) {
                console.log("Total success chance: %.2f, (%.2f before clamping).", g, f)
            } else {
                console.log("Total success chance: %.2f.", g)
            }
            console.log("----- End ComputeSuccessChance -----")
        }
        return {
            chance: Math.floor(g),
            chanceOver: f - g
        }
    },
    picker: {
        curSide: "",
        onFollowerPickerShow: function(k, g, b) {
            Lightbox.setSize(800, 564);
            var e;
            var l = function(n) {
                var m = {};
                for (var a in this.followers) {
                    if (this.followers.hasOwnProperty(a) && this.followers[a].follower.hasOwnProperty("id")) {
                        m[this.followers[a].follower.id] = 1
                    }
                }
                var d = m.hasOwnProperty(n.id);
                if (this.currentProfile) {
                    d |= !this.profiler[this.currentProfile].hasOwnProperty(n.id)
                }
                return !d
            };
            if (g) {
                this.picker.curSide = this.side;
                k.className = "listview";
                k.style.borderWidth = "10px";
                k.style.borderStyle = "solid";
                k.style.borderColor = "#404040";
                k.style.backgroundColor = "#404040";
                k.style.borderBottomWidth = "0";
                var h = $WH.ce("div"),
                    j = $WH.ce("a"),
                    f = $WH.ce("div");
                h.className = "listview";
                h.style.borderWidth = "0";
                $WH.ae(k, h);
                j = $WH.ce("a");
                j.className = "dialog-x";
                j.href = "javascript:;";
                j.onclick = Lightbox.hide;
                $WH.ae(j, $WH.ct(LANG.close));
                $WH.ae(k, j);
                f.className = "clear";
                $WH.ae(k, f);
                var c = [];
                for (var i in g_garrison_followers) {
                    if (g_garrison_followers.hasOwnProperty(i)) {
                        c.push(g_garrison_followers[i])
                    }
                }
                e = new Listview({
                    template: "followerPicker",
                    _calc: this,
                    id: "followers",
                    parent: h,
                    clip: {
                        h: 451
                    },
                    note: "",
                    data: c,
                    customFilter: l.bind(this)
                });
                if ($WH.Browser.firefox) {
                    $WH.aE(e.getClipDiv(), "DOMMouseScroll", g_pickerWheel)
                } else {
                    e.getClipDiv().onmousewheel = g_pickerWheel
                }
            } else {
                e = g_listviews.followers;
                if (this.picker.curSide != this.side) {
                    this.picker.curSide = this.side
                }
                e.clearSearch();
                e.updateFilters(true);
                e.indexCreated = false;
                e.applySort();
                e.refreshRows(true)
            }
            $WH.ee(e.thead);
            e.createHeader();
            e.updateSortArrow();
            setTimeout(function() {
                e.quickSearchBox.focus()
            }, 1)
        },
        chosenFollower: function(c) {
            Lightbox.hide();
            var b = this.followers[this.choosingSlot];
            b.follower = c;
            delete b.chosenLevel;
            delete b.chosenItemLevel;
            delete b.chosenQuality;
            delete b.chosenAbilities;
            if (this.currentProfile) {
                var a = this.profiler[this.currentProfile][c.id];
                b.chosenLevel = a.level;
                b.chosenItemLevel = a.avgilvl;
                b.chosenQuality = a.quality;
                b.chosenAbilities = a.abilities.slice()
            } else {
                if (c.hasOwnProperty(this.side) && c[this.side].hasOwnProperty("abilities")) {
                    b.chosenAbilities = c[this.side].abilities.slice()
                }
            }
            this.UpdateFollowerDisplay(this.choosingSlot);
            this.choosingSlot = undefined
        },
        onAbilityPickerShow: function(l, g, b) {
            Lightbox.setSize(800, 564);
            var e;
            var k = function(o) {
                var m = {};
                var n = this.followers[this.choosingSlot];
                if (n.chosenAbilities) {
                    for (var a = 0; a < n.chosenAbilities.length; a++) {
                        if (a == this.choosingAbility.index) {
                            continue
                        }
                        m[n.chosenAbilities[a]] = 1
                    }
                }
                var d = false;
                d = m.hasOwnProperty(o.id);
                d |= (this.choosingAbility.allowed & (o.trait ? 2 : 1)) == 0;
                d |= (o.side && o.side != this.side);
                d |= (o.followerclass.length && ($WH.in_array(o.followerclass, n.follower[this.side].classid) < 0));
                return !d
            };
            if (g) {
                this.picker.curSide = this.side;
                l.className = "listview";
                l.style.borderWidth = "10px";
                l.style.borderStyle = "solid";
                l.style.borderColor = "#404040";
                l.style.backgroundColor = "#404040";
                l.style.borderBottomWidth = "0";
                var h = $WH.ce("div"),
                    j = $WH.ce("a"),
                    f = $WH.ce("div");
                h.className = "listview";
                h.style.borderWidth = "0";
                $WH.ae(l, h);
                j = $WH.ce("a");
                j.className = "dialog-x";
                j.href = "javascript:;";
                j.onclick = Lightbox.hide;
                $WH.ae(j, $WH.ct(LANG.close));
                $WH.ae(l, j);
                f.className = "clear";
                $WH.ae(l, f);
                var c = [];
                for (var i in g_garrison_abilities) {
                    if (g_garrison_abilities.hasOwnProperty(i)) {
                        c.push(g_garrison_abilities[i])
                    }
                }
                e = new Listview({
                    template: "abilityPicker",
                    _calc: this,
                    id: "abilities",
                    parent: h,
                    clip: {
                        h: 451
                    },
                    note: "",
                    data: c,
                    customFilter: k.bind(this)
                });
                if ($WH.Browser.firefox) {
                    $WH.aE(e.getClipDiv(), "DOMMouseScroll", g_pickerWheel)
                } else {
                    e.getClipDiv().onmousewheel = g_pickerWheel
                }
            } else {
                e = g_listviews.abilities;
                if (this.picker.curSide != this.side) {
                    this.picker.curSide = this.side
                }
                e.clearSearch();
                e.updateFilters(true);
                e.refreshRows(true)
            }
            $WH.ee(e.thead);
            e.createHeader();
            e.updateSortArrow();
            setTimeout(function() {
                e.quickSearchBox.focus()
            }, 1)
        },
        chosenAbility: function(b) {
            Lightbox.hide();
            var a = this.followers[this.choosingSlot];
            if (this.choosingAbility.index >= 0) {
                a.chosenAbilities[this.choosingAbility.index] = b.id
            } else {
                if (!a.chosenAbilities) {
                    a.chosenAbilities = []
                }
                a.chosenAbilities.push(b.id)
            }
            this.UpdateFollowerDisplay(this.choosingSlot);
            this.choosingSlot = undefined;
            this.choosingAbility = undefined
        }
    }
};
jQuery.extend(Listview.templates, {
    followerPicker: {
        sort: [1],
        nItemsPerPage: -1,
        hideBands: 2,
        hideNav: 1 | 2,
        searchable: 1,
        searchDelay: 100,
        poundable: 0,
        filtrable: 0,
        clip: {
            w: 800,
            h: 486
        },
        columns: [{
            id: "name",
            name: LANG.name,
            type: "text",
            align: "left",
            compute: function(e, c, g) {
                var i = this._calc.currentProfile ? this._calc.profiler[this._calc.currentProfile][e.id] : e;
                var j = i.quality;
                var h = $WH.ce("a");
                h.className = "q" + j + " listview-cleartext";
                h.href = "/follower=" + e.id + "." + (this._calc.side == "alliance" ? 1 : 2);
                if (this._calc.currentProfile) {
                    h.rel = "q=" + j + "&level=" + i.level + "&abil=" + i.abilities.join(":")
                }
                c.style.paddingLeft = "2.5em";
                if (e[this._calc.side].portrait) {
                    c.style.backgroundImage = 'url("' + g_staticUrl + "/images/wow/garr/" + e[this._calc.side].portrait + '.png")';
                    c.style.backgroundRepeat = "no-repeat";
                    c.style.backgroundSize = "3em 3em";
                    c.style.backgroundPosition = "-0.75em -0.25em"
                }
                $WH.ae(h, $WH.ct(e[this._calc.side].name));
                var b = $WH.ce("div");
                $WH.ae(c, b);
                $WH.ae(b, h);
                if (this._calc.currentProfile && !this._calc.profiler[this._calc.currentProfile][e.id].active) {
                    b.style.position = "relative";
                    var f = $WH.ce("div");
                    f.className = "listview-name-info q10";
                    $WH.ae(f, $WH.ct(LANG.inactive));
                    $WH.aef(b, f)
                }
                var k = this;
                $(g).click(function(a) {
                    if (a.which != 2 || a.target != h) {
                        a.preventDefault();
                        k._calc.picker.chosenFollower.call(k._calc, e)
                    }
                })
            },
            getVisibleText: function(a) {
                return a[g_listviews.followers._calc.side].name
            },
            sortFunc: function(d, c) {
                return $WH.strcmp(d[g_listviews.followers._calc.side].name, c[g_listviews.followers._calc.side].name)
            }
        }, {
            id: "class",
            name: LANG.classs,
            type: "text",
            align: "left",
            width: "20%",
            compute: function(b, c, a) {
                $WH.st(c, b[this._calc.side].classs)
            },
            getVisibleText: function(a) {
                return a[g_listviews.followers._calc.side].classs
            },
            sortFunc: function(d, c) {
                return $WH.strcmp(d[g_listviews.followers._calc.side].classs, c[g_listviews.followers._calc.side].classs)
            }
        }, {
            id: "level",
            name: LANG.level,
            type: "number",
            compute: function(b, d) {
                var c = b.level;
                var a = Math.floor((b.itemlevelarmor + b.itemlevelweapon) / 2);
                if (this._calc.currentProfile) {
                    c = this._calc.profiler[this._calc.currentProfile][b.id].level;
                    a = this._calc.profiler[this._calc.currentProfile][b.id].avgilvl
                }
                $WH.st(d, "" + c + (c >= this._calc.maxLevel ? $WH.sprintf(LANG.qty, a) : ""))
            },
            sortFunc: function(d, c) {
                var i = d.level,
                    g = c.level,
                    f = Math.floor((d.itemlevelarmor + d.itemlevelweapon) / 2),
                    e = Math.floor((c.itemlevelarmor + c.itemlevelweapon) / 2);
                if (this._calc.currentProfile) {
                    var h = this._calc.profiler[this._calc.currentProfile];
                    if (h[d.id]) {
                        i = h[d.id].level;
                        f = h[d.id].avgilvl
                    }
                    if (h[c.id]) {
                        g = h[c.id].level;
                        e = h[c.id].avgilvl
                    }
                }
                return $WH.strcmp(i, g) || $WH.strcmp(f, e)
            }
        }, {
            id: "abilities",
            name: LANG.tab_abilities,
            compute: function(f, g, e) {
                g.style.padding = "0px";
                var c = this._calc.currentProfile ? this._calc.profiler[this._calc.currentProfile][f.id].abilities : f[this._calc.side].abilities;
                for (var a = 0; a < c.length; a++) {
                    if (!g_garrison_abilities.hasOwnProperty(c[a])) {
                        continue
                    }
                    var d = g_garrison_abilities[c[a]];
                    var b = Icon.create(d.icon, 0, null, "/garrisonability=" + d.id);
                    b.className += " garrison-ability";
                    b.style.display = "inline-block";
                    $WH.ae(g, b)
                }
            },
            getVisibleText: function(e) {
                var c = "";
                var b = g_listviews.followers._calc.side;
                var d = g_listviews.followers._calc.currentProfile ? g_listviews.followers._calc.profiler[g_listviews.followers._calc.currentProfile][e.id].abilities : e[b].abilities;
                for (var a = 0; a < d.length; a++) {
                    if (!g_garrison_abilities.hasOwnProperty(d[a])) {
                        continue
                    }
                    c += " " + g_garrison_abilities[d[a]].name
                }
                return c
            },
            sortFunc: function(d, c) {
                var e = d[this._calc.side].abilities;
                var f = c[this._calc.side].abilities;
                if (this._calc.currentProfile) {
                    if (this._calc.profiler[this._calc.currentProfile][d.id]) {
                        e = this._calc.profiler[this._calc.currentProfile][d.id].abilities
                    }
                    if (this._calc.profiler[this._calc.currentProfile][c.id]) {
                        f = this._calc.profiler[this._calc.currentProfile][c.id].abilities
                    }
                }
                return e.length - f.length
            }
        }]
    },
    abilityPicker: {
        sort: [1, 2],
        nItemsPerPage: -1,
        hideBands: 2,
        hideNav: 1 | 2,
        searchable: 1,
        searchDelay: 100,
        poundable: 0,
        filtrable: 0,
        clip: {
            w: 800,
            h: 486
        },
        columns: [{
            id: "name",
            name: LANG.name,
            type: "text",
            align: "left",
            value: "name",
            span: 2,
            compute: function(f, g, e) {
                var d = $WH.ce("td");
                d.style.width = "1px";
                d.style.paddingRight = "0";
                d.style.borderRight = "none";
                $WH.ae(d, Icon.create(f.icon, (this.iconSize == null ? 0 : this.iconSize), null, "javascript:;"));
                $WH.ae(e, d);
                g.style.borderLeft = "none";
                var b = $WH.ce("span");
                b.className = "listview-cleartext";
                $WH.ae(b, $WH.ct(f.name));
                $WH.ae(g, b);
                var c = this;
                $(e).click(function(a) {
                    if (a.which != 2 || a.target != b) {
                        a.preventDefault();
                        c._calc.picker.chosenAbility.call(c._calc, f)
                    }
                })
            },
            getVisibleText: function(a) {
                return a.name
            }
        }, {
            id: "description",
            name: LANG.description,
            type: "text",
            align: "left",
            compute: function(a, c) {
                var b = $WH.ce("div");
                b.className = "small";
                $WH.ae(b, $WH.ct(a.description));
                $WH.ae(c, b)
            },
            sortFunc: function(d, c) {
                return $WH.strcmp(d.description, c.description)
            }
        }, {
            id: "type",
            name: LANG.type,
            type: "text",
            compute: function(b, c) {
                var a = b.flags & 1;
                $WH.st(c, g_garrison_ability_types[b.trait ? 1 : 0])
            },
            getVisibleText: function(a) {
                return g_garrison_ability_types[a.trait ? 1 : 0]
            },
            sortFunc: function(d, c) {
                return $WH.strcmp(g_garrison_ability_types[d.trait ? 1 : 0], g_garrison_ability_types[c.trait ? 1 : 0])
            }
        }, {
            id: "category",
            name: LANG.category,
            type: "text",
            hidden: "true",
            compute: function(a, b) {
                if (g_garrison_ability_categories.hasOwnProperty(a.category)) {
                    $WH.st(b, g_garrison_ability_categories[a.category])
                }
            },
            getVisibleText: function(a) {
                if (g_garrison_ability_categories.hasOwnProperty(a.category)) {
                    return g_garrison_ability_categories[a.category]
                }
                return ""
            },
            sortFunc: function(d, c) {
                return $WH.strcmp(g_garrison_ability_categories.hasOwnProperty(d.category) ? g_garrison_ability_categories[d.category] : "", g_garrison_ability_categories.hasOwnProperty(c.category) ? g_garrison_ability_categories[c.category] : "")
            }
        }, {
            id: "counters",
            name: LANG.counters,
            type: "text",
            span: 2,
            align: "left",
            compute: function(e, c, h) {
                if (!e.hasOwnProperty("counters")) {
                    return
                }
                var k = 0;
                var f = $WH.ce("td");
                f.style.width = "1px";
                f.style.paddingRight = "0";
                f.style.borderRight = "none";
                $WH.ae(h, f);
                c.style.borderLeft = "none";
                c.style.paddingLeft = "0";
                c.style.lineHeight = "26px";
                for (var j in e.counters) {
                    if (!e.counters.hasOwnProperty(j)) {
                        continue
                    }
                    var b = e.counters[j];
                    if (!b) {
                        continue
                    }
                    var a = g_garrison_mechanics[b];
                    var g = $WH.ce("div");
                    g.style.display = "inline-block";
                    if (a.hasOwnProperty("icon")) {
                        $WH.ae(f, Icon.create(a.icon, 0, null, "javascript:;"))
                    }
                    var l = $WH.ce("span");
                    l.style.whiteSpace = "nowrap";
                    $WH.st(l, a.name);
                    $WH.ae(g, l);
                    if (a.hasOwnProperty("description") && a.description != "") {
                        $WH.Tooltip.simple(l, a.description);
                        l.className = "tip"
                    }
                    if (k++ > 0) {
                        $WH.ae(c, $WH.ce("br"))
                    }
                    $WH.ae(c, g)
                }
            },
            getVisibleText: function(b) {
                var a = "";
                for (var c in b.counters) {
                    if (!b.counters.hasOwnProperty(c)) {
                        continue
                    }
                    if (!b.counters[c]) {
                        continue
                    }
                    a += g_garrison_mechanics[b.counters[c]].name + " "
                }
                return a
            },
            sortFunc: function(e, d) {
                var f = this.getVisibleText(e);
                var c = this.getVisibleText(d);
                return $WH.strcmp(f, c)
            }
        }]
    }
});
var HashClass = function(n) {
    var b = {
        b64array: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_="
    };
    var p = this;
    var e = n;
    var i = 0;
    var l = 0;
    this.anchor = undefined;
    var q;
    this.Read = function() {
        var y, u, E;
        if (i > l) {
            l++;
            return false
        }
        if (i != l) {
            return false
        }
        var H = location.hash;
        if (H.substr(0, 1) == "#") {
            H = H.substr(1)
        }
        var F = (H.length > 1) && (!/[^A-Za-z0-9\-_]/g.exec(H));
        u = F ? a(H) : [0];
        if (u.length < 3) {
            return false
        }
        var z = [u.shift(), u.shift(), u.shift()];
        if (u.length < z[0]) {
            return false
        }
        while ((u.length > z[0]) && (u[u.length - 1] == 0)) {
            u.pop()
        }
        if (j(z) != j(d(u))) {
            return false
        }
        E = g(u);
        y = E.shift();
        switch (y) {
            case 1:
            case 2:
                e.skipHash++;
                var s = "alliance",
                    K = 0;
                var w = false,
                    G = false;
                if (y == 1) {
                    var r = E.shift();
                    K = r & 7;
                    s = (r & 8) ? "horde" : "alliance"
                } else {
                    if (y == 2) {
                        var A = E.shift();
                        s = (A & 1) ? "horde" : "alliance";
                        G = !!(A & 2);
                        w = !!(A & 4);
                        K = E.shift()
                    }
                }
                var J = 0;
                for (var I in e.followers) {
                    e.followers[I].follower = {};
                    delete e.followers[I].chosenLevel;
                    delete e.followers[I].chosenItemLevel;
                    delete e.followers[I].chosenQuality;
                    delete e.followers[I].chosenAbilities;
                    J++;
                    e.UpdateFollowerDisplay(I)
                }
                e.SetSide(s);
                e.forceMaxlevelInput.checked = w;
                e.fastestInput.checked = G;
                for (var v = 1; v <= K && v <= J; v++) {
                    var t = (E.shift() << 8) | (E.shift() << 4) | E.shift();
                    var D = e.followers[v];
                    if (!g_garrison_followers.hasOwnProperty(t)) {
                        D = {}
                    }
                    D.follower = g_garrison_followers[t];
                    D.chosenQuality = E.shift();
                    D.chosenLevel = E.shift() + 90;
                    if (D.chosenLevel >= 100) {
                        D.chosenItemLevel = ((E.shift() << 4) | E.shift()) + 600
                    } else {
                        D.chosenItemLevel = e.minItemLevel.hasOwnProperty(D.chosenQuality) ? e.minItemLevel[D.chosenQuality] : e.minItemLevel["default"]
                    }
                    var B = o(E.splice(0, E.shift() * 2));
                    D.chosenAbilities = [];
                    for (var C = 0; C < B.length; C++) {
                        if (g_garrison_abilities.hasOwnProperty(B[C])) {
                            D.chosenAbilities.push(B[C])
                        }
                    }
                    e.UpdateFollowerDisplay(v)
                }
                e.skipHash--;
                break;
            default:
                p.Create();
                return false
        }
        return true
    };
    this.Create = function() {
        if (q) {
            window.clearTimeout(q)
        }
        q = window.setTimeout(c, 500)
    };

    function c() {
        var s;
        s = k();
        q = 0;
        var r = location.hash;
        if (r.substr(0, 1) == "#") {
            r = r.substr(1)
        }
        if (r != s) {
            if (p.anchor) {
                p.anchor.href = "#" + s
            }
            if (s) {
                i++;
                location.hash = "#" + s
            }
        }
    }

    function k() {
        var y = [],
            t, B;
        var w = [];
        for (var s in e.followers) {
            if (e.followers.hasOwnProperty(s) && e.followers[s].follower.hasOwnProperty("id")) {
                w.push(s)
            }
        }
        if (w.length == 0) {
            return ""
        }
        y.push(2);
        y.push((e.forceMaxlevelInput.checked ? 4 : 0) | (e.fastestInput.checked ? 2 : 0) | (e.side == "alliance" ? 0 : 1));
        y.push(w.length);
        for (var r = 0, A; A = w[r]; r++) {
            var u = e.followers[A];
            y.push((u.follower.id & 3840) >> 8);
            y.push((u.follower.id & 240) >> 4);
            y.push((u.follower.id & 15));
            y.push(parseInt(u.chosenQuality));
            y.push(parseInt(u.chosenLevel) - 90);
            if (parseInt(u.chosenLevel) >= 100) {
                y.push(((parseInt(u.chosenItemLevel) - 600) & 240) >> 4);
                y.push(((parseInt(u.chosenItemLevel) - 600) & 15))
            }
            if (u.chosenAbilities.length) {
                t = [];
                B = [];
                for (var z = 0; z < u.chosenAbilities.length; z++) {
                    f(t, B, parseInt(u.chosenAbilities[z]))
                }
                if ((t.length > 0) && (t.length < 4)) {
                    f(t, B, 0, true)
                }
                if (B.length % 2 == 1) {
                    B.push(0)
                }
                y.push(B.length / 2);
                y = y.concat(B)
            } else {
                y.push(0)
            }
        }
        var C = m(y);
        var v = d(C);
        for (var z = v.length - 1; z >= 0; z--) {
            C.unshift(v[z])
        }
        return h(C)
    }

    function f(u, v, t, s) {
        var w = 0;
        if (!s) {
            u.push(parseInt(t))
        }
        if ((u.length == 4) || s) {
            for (var r = 0; r < u.length; r++) {
                w = (w << 1) | (u[r] > 255 ? 1 : 0)
            }
            w = (w << (u.length - 4));
            v.push(w);
            while (u.length > 0) {
                var t = u.shift();
                if (t > 255) {
                    v.push((t & 3840) >> 8)
                }
                v.push((t & 240) >> 4);
                v.push(t & 15)
            }
        }
    }

    function m(u) {
        var t = [];
        var s;
        for (var r = 0; r < u.length; r += 2) {
            s = u[r] << 4;
            if (r + 1 < u.length) {
                s |= u[r + 1]
            }
            t.push(s)
        }
        return t
    }

    function o(v) {
        var u, s, t = [],
            r = 0;
        while (r < v.length) {
            s = v[r++];
            for (var w = 0; w <= 3; w++) {
                u = 0;
                if ((s >> (3 - w)) & 1 > 0) {
                    u = (v[r++] << 8)
                }
                u |= (v[r++] << 4) | (v[r++]);
                t.push(u);
                if (r >= v.length) {
                    break
                }
            }
        }
        return t
    }

    function g(s) {
        var t = [];
        for (var r = 0; r < s.length; r++) {
            t.push(s[r] >> 4);
            t.push(s[r] & 15)
        }
        return t
    }

    function j(r) {
        var t = 0;
        for (var s = (r.length - 1); s >= 0; s--) {
            t = t * 256 + r[s]
        }
        return t
    }

    function d(s) {
        var u = 0,
            t = 0;
        for (var r = 0; r < s.length; r++) {
            u = (u + s[r]) % 255;
            t = (t + u) % 255
        }
        return [s.length, t, u]
    }

    function h(A) {
        try {
            if (window.btoa) {
                return window.btoa(String.fromCharCode.apply(null, A)).replace(new RegExp("\\+", "g"), b.b64array[62]).replace(new RegExp("/", "g"), b.b64array[63]).replace(/=+$/, "")
            }
        } catch (t) {
            void(0)
        }
        var r = "";
        var B, y, w = 0;
        var C, z, v, u = "";
        var s = 0;
        while (s < A.length) {
            B = (s >= A.length) ? NaN : A[s++];
            y = (s >= A.length) ? NaN : A[s++];
            w = (s >= A.length) ? NaN : A[s++];
            C = B >> 2;
            z = ((B & 3) << 4) | (y >> 4);
            v = ((y & 15) << 2) | (w >> 6);
            u = w & 63;
            if (isNaN(y)) {
                v = u = 64
            } else {
                if (isNaN(w)) {
                    u = 64
                }
            }
            r = r + b.b64array.charAt(C) + b.b64array.charAt(z) + b.b64array.charAt(v) + b.b64array.charAt(u);
            B = y = w = 0;
            C = z = v = u = ""
        }
        if (r.indexOf(b.b64array.charAt(64)) > 0) {
            r = r.substr(0, r.indexOf(b.b64array.charAt(64)))
        }
        return r
    }

    function a(t) {
        var u = [];
        try {
            if (window.atob) {
                var s = window.atob(t.replace(new RegExp(b.b64array[62], "g"), "+").replace(new RegExp(b.b64array[63], "g"), "/").concat("===".substr(0, t.length % 4)));
                for (var w = 0; w < s.length; w++) {
                    u.push(s.charCodeAt(w))
                }
                return u
            }
        } catch (v) {
            void(0)
        }
        var E, C, A = 0;
        var D, B, z, y = -1;
        var r = 0;
        while (r < t.length) {
            D = b.b64array.indexOf(t.charAt(r++));
            B = b.b64array.indexOf(t.charAt(r++));
            z = b.b64array.indexOf(t.charAt(r++));
            y = b.b64array.indexOf(t.charAt(r++));
            if ((D > -1) && (B > -1)) {
                E = (D << 2) | (B >> 4);
                C = ((B & 15) << 4) | (z >> 2);
                A = ((z & 3) << 6) | y;
                u.push(E);
                if (z != -1) {
                    u.push(C)
                }
                if (y != -1) {
                    u.push(A)
                }
            }
            E = C = A = 0;
            D = B = z = y = -1
        }
        return u
    }
};