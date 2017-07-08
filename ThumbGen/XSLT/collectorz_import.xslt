<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
	<xsl:output method="xml" indent="yes"/>
	<xsl:template match="//movieinfo/movielist">
		<xsl:element name="movies">
			<xsl:call-template name="movie"/>
		</xsl:element>
	</xsl:template>
	<xsl:template name="movie">
		<xsl:for-each select="movie">
			<xsl:element name="movie">
				<xsl:attribute name="ThumbGen"><xsl:text>1</xsl:text></xsl:attribute>
				<xsl:element name="title">
					<xsl:value-of select="title"/>
				</xsl:element>
				<xsl:element name="originaltitle">
					<xsl:value-of select="originaltitle"/>
				</xsl:element>
				<xsl:element name="year">
					<xsl:value-of select="releasedate/year/displayname"/>
				</xsl:element>
				<xsl:element name="plot">
					<xsl:value-of select="plot"/>
				</xsl:element>
				<xsl:element name="rating">
					<xsl:value-of select="imdbrating"/>
				</xsl:element>
				<xsl:element name="releasedate">
					<xsl:value-of select="releasedate/date"/>
				</xsl:element>
				<xsl:element name="id">
					<xsl:text>tt</xsl:text>
					<xsl:call-template name="padleft">
						<xsl:with-param name="padVar" select="imdbnum"/>
						<xsl:with-param name="length" select="7"/>
					</xsl:call-template>
					<!--xsl:value-of select="imdbnum"/-->
				</xsl:element>
				<xsl:element name="actor">
					<xsl:call-template name="actor"/>
				</xsl:element>
				<xsl:element name="genre">
					<xsl:call-template name="genre"/>
				</xsl:element>
				<xsl:element name="director">
					<xsl:call-template name="director"/>
				</xsl:element>
				<xsl:element name="runtime">
					<xsl:value-of select="runtimeminutes"/>
				</xsl:element>
				<xsl:element name="comments">
					<xsl:value-of select="notes"/>
				</xsl:element>

				<xsl:element name="mpaa">
                </xsl:element>
				<xsl:element name="certification">
					<xsl:value-of select="mpaarating/displayname"/>
				</xsl:element>
				<xsl:element name="studio">
					<xsl:call-template name="studio"/>
				</xsl:element>
				<xsl:element name="country">
					<xsl:element name="name">
						<xsl:value-of select="country/displayname"/>
					</xsl:element>
				</xsl:element>
				<xsl:element name="localfiles">
					<xsl:call-template name="localfiles"/>
				</xsl:element>
				<xsl:element name="cover">
					<xsl:value-of select="coverfront"/>
				</xsl:element>
				<xsl:element name="background">
					<xsl:value-of select="coverback"/>
				</xsl:element>
			</xsl:element>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="actor">
		<xsl:for-each select="cast/star[roleid = 'dfActor']/person">
			<xsl:element name="name">
				<xsl:value-of select="displayname"/>
			</xsl:element>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="director">
		<xsl:for-each select="crew/crewmember[roleid = 'dfDirector']/person">
			<xsl:element name="name">
				<xsl:value-of select="displayname"/>
			</xsl:element>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="genre">
		<xsl:for-each select="genres/genre">
			<xsl:element name="name">
				<xsl:value-of select="displayname"/>
			</xsl:element>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="studio">
		<xsl:for-each select="studios/studio">
			<xsl:element name="name">
				<xsl:value-of select="displayname"/>
			</xsl:element>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="localfiles">
		<xsl:for-each select="links/link[urltype='Movie']">
			<xsl:element name="name">
				<xsl:value-of select="url"/>
			</xsl:element>
		</xsl:for-each>
	</xsl:template>
	<xsl:template name="padleft">
		<!-- recursive template to right justify and prepend the value with whatever padChar is passed in   -->
		<xsl:param name="padChar">0</xsl:param>
		<xsl:param name="padVar"/>
		<xsl:param name="length"/>
		<xsl:choose>
			<xsl:when test="string-length($padVar) &lt; $length">
				<xsl:call-template name="padleft">
					<xsl:with-param name="padChar" select="$padChar"/>
					<xsl:with-param name="padVar" select="concat($padChar,$padVar)"/>
					<xsl:with-param name="length" select="$length"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="substring($padVar,string-length($padVar) - $length + 1)"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template name="genreold">
		<xsl:call-template name="divide">
			<xsl:with-param name="to-be-divided" select="genre"/>
			<xsl:with-param name="delimiter" select="'/'"/>
		</xsl:call-template>
	</xsl:template>
	<xsl:template name="divide">
		<xsl:param name="to-be-divided"/>
		<xsl:param name="delimiter"/>
		<xsl:choose>
			<xsl:when test="contains($to-be-divided,$delimiter)">
				<name>
					<xsl:value-of select="translate(substring-before($to-be-divided,$delimiter) ,' ', '') "/>
				</name>
				<xsl:call-template name="divide">
					<xsl:with-param name="to-be-divided" select="substring-after($to-be-divided,$delimiter)"/>
					<xsl:with-param name="delimiter" select="'/'"/>
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<name>
					<xsl:value-of select="translate($to-be-divided, ' ', '' )"/>
				</name>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
</xsl:stylesheet>
